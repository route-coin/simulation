using System;
using System.Threading.Tasks;
using System.Numerics;
using System.Configuration;
using Microsoft.ServiceBus.Messaging;
using System.Collections.Generic;
using DatabaseRepository;
using ServiceBusRepository;
using Nethereum.Hex.HexTypes;
using System.Linq;
using System.IO;
using EthereumRepository;

namespace RouteCoin
{
    public class Program
    {
        public static Node node { get; set; }
        private static Node baseStationNode { get; set; }

        private static int ContractGracePeriod = 10;
        private static BigInteger InitialContractBalance = 60000; // 
        private static int CoverageArea = 25; // each node covers 20 meters around it

        private static string RouteCoinSubscriptionName = "RouteCoinMessages";

        static void Main(string[] args)
        {
            baseStationNode = DatabaseHelper.GetBaseStation();

            node = DatabaseHelper.DedicateNode();

            if (node == null)
            {
                DatabaseHelper.Log("No Node","No free node to be dedicated. all nodes are running");
                Console.Write("Press any key to exit");
                Console.Read();
                return;
            }

            //var helper = new ContractHelper();
            //var result = helper.test(node.PublicKey, node.Password);

            ServiceBusHelper.SubscribeToTopic(node.PublicKey);

            var connectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];

            var Client = SubscriptionClient.CreateFromConnectionString(connectionString, node.PublicKey, RouteCoinSubscriptionName);

            // Configure the callback options.
            OnMessageOptions options = new OnMessageOptions();
            options.AutoComplete = false;
            options.AutoRenewTimeout = TimeSpan.FromMinutes(10);

            Client.OnMessage((message) =>
            {
                try
                {
                    if (ProcessMessage(message))
                        message.Complete();
                    else
                        message.Abandon();
                }
                catch (Exception ex)
                {
                    DatabaseHelper.Log(node.PublicKey, $"Error:{ex.Message}");
                    message.Abandon();
                }
            }, options);

            Console.WriteLine("Press any key to exit");
            Console.Read();

            DatabaseHelper.ReleaseNode(node);
        }

        private static bool ProcessMessage(BrokeredMessage message)
        {
            var body = message.GetBody<WhisperMessage>();
            var contractHelper = new ContractHelper();
            var balance = new HexBigInteger(100000);
            var contractAddress = string.Empty;
            var parent = string.Empty;
            var buyer = string.Empty;

            DatabaseHelper.Log(node.PublicKey, $"Processing Recieved Message: { body.Subject }");

            switch (body.Subject)
            {
                case WhisperMessage.State.CreateContract:
                    if (!IsBaseStationClose())
                    {
                        // initial contract, so all parent contract addresses will be 0x
                        var parentContract = "0x0";
                        contractAddress = contractHelper.CreateContract(node.PublicKey, node.Password, balance, baseStationNode.PublicKey, ContractGracePeriod, parentContract);
                        SaveContractLocally(contractAddress, string.Empty);
                        SendContractCreatedMessageToNeighborNodes(contractAddress, string.Empty);
                    }
                    else
                    {
                        DatabaseHelper.Log(node.PublicKey, $"Base Station is close to this node. No need to create a contract. Node: { node.PublicKey }");
                    }
                    break;

                case WhisperMessage.State.ContractCreated:

                    parent = contractHelper.GetParentContract(node.PublicKey, node.Password, body.ContractAddress);
                    if (!IsBaseStationClose())
                    {
                        DatabaseHelper.Log(node.PublicKey, $"Base station is not close to this node. Creating additional contracts. Incoming contract: {body.ContractAddress}");

                        var parentContractBalance = contractHelper.GetBalance(node.PublicKey, node.Password, body.ContractAddress);
                        //if (!AlreadyInvolvedInThisContractChain(body.ContractAddress, parent))
                        //{
                            contractAddress = contractHelper.CreateContract(node.PublicKey, node.Password, new HexBigInteger(parentContractBalance/2), baseStationNode.PublicKey, ContractGracePeriod, body.ContractAddress);
                            SaveContractLocally(contractAddress, body.ContractAddress);
                            SendContractCreatedMessageToNeighborNodes(contractAddress, body.FromAddress);
                        //}
                        //else
                        //{
                        //    DatabaseHelper.Log($"Already involved in this contract chain. {contractAddress}");
                        //}
                    }
                    else
                    {
                        DatabaseHelper.Log(node.PublicKey, $"Base station is close to this node. Set the contract to RouteFound state. Incoming contract: {body.ContractAddress}.");
                        var routeFoundSubmitted = contractHelper.RouteFound(node.PublicKey, node.Password, body.ContractAddress, node.PublicKey, parent);
                        if (!string.IsNullOrEmpty(routeFoundSubmitted))
                        {
                             buyer = contractHelper.GetBuyer(node.PublicKey, node.Password, body.ContractAddress);
                             DatabaseHelper.Log(node.PublicKey, $"RouteFound transaction submitted. Contract: { body.ContractAddress }. Seller: {node.PublicKey}");
                             ServiceBusHelper.SendMessageToTopic(node, new Node() { PublicKey = buyer }, baseStationNode, body.ContractAddress, WhisperMessage.State.RouteFound, null);
                             DatabaseHelper.Log(node.PublicKey, $"RouteFound whisper sent o buyer. Contract: { body.ContractAddress }. Buyer: {buyer}. Seller: {node.PublicKey}");
                        }
                        else
                        {
                             DatabaseHelper.Log(node.PublicKey, $"RouteFound transaction was not submitted successfully. Contract: { parent }");
                        }
                    }

                    break;

                case WhisperMessage.State.RouteFound:
                    parent = contractHelper.GetParentContract(node.PublicKey, node.Password, body.ContractAddress);
                    if (parent != "0x0000000000000000000000000000000000000000" && parent != string.Empty && parent != "0x" && parent != "0x0")
                    {
                        var parentsParent = contractHelper.GetParentContract(node.PublicKey, node.Password, parent);
                        var routeFoundSubmitted = contractHelper.RouteFound(node.PublicKey, node.Password, parent, node.PublicKey, parentsParent);
                        if (!string.IsNullOrEmpty(routeFoundSubmitted))
                        {
                            buyer = contractHelper.GetBuyer(node.PublicKey, node.Password, parent);
                            DatabaseHelper.Log(node.PublicKey, $"RouteFound transaction submitted. Contract: { parent }. Seller: {node.PublicKey}");
                            ServiceBusHelper.SendMessageToTopic(node, new Node() { PublicKey = buyer }, baseStationNode, parent, WhisperMessage.State.RouteFound, null);
                            DatabaseHelper.Log(node.PublicKey, $"RouteFound whisper sent o buyer. Contract: { parent }. Buyer: {buyer}. Seller: {node.PublicKey}");
                        }
                        else
                        {
                            DatabaseHelper.Log(node.PublicKey, $"RouteFound transaction was not submitted successfully. Contract: { parent }");
                        }
                    }
                    else  // when node with no parent gets the RouteFound, it is the initial node that created the contract chain. so should send RouteConfirm Whisper to the seller node.
                    {
                        ServiceBusHelper.SendMessageToTopic(node, new Node() { PublicKey = body.FromAddress }, baseStationNode, body.ContractAddress, WhisperMessage.State.RouteConfirmed, new List<string> { body.ContractAddress });
                        DatabaseHelper.Log(node.PublicKey, $"RouteConfirm whisper sent to seller. Contract: { body.ContractAddress }. Buyer: {buyer}.");
                    }

                    break;

                case WhisperMessage.State.RouteConfirmed:
                    if (node.PublicKey == baseStationNode.PublicKey) // it is base station node, so confirm all the contracts came from the message
                    {
                        DatabaseHelper.Log(node.PublicKey, $"Node is base station. confirming all contracts in the chain.");
                        DatabaseHelper.Log(node.PublicKey, $"Contracts to be confirmed: {body.ContractsChain.Count}.");

                        foreach (var contract in body.ContractsChain)
                        {
                            // TODO: what if some of these don't work?
                            // Should we try to abort the confirmed ones?
                            parent = contractHelper.GetParentContract(node.PublicKey, node.Password, contract);
                            var routeConfirmSubmitted = contractHelper.RouteConfirmed(node.PublicKey, node.Password, contract, node.PublicKey, parent);
                            if (!string.IsNullOrEmpty(routeConfirmSubmitted))
                            {
                                DatabaseHelper.Log(node.PublicKey, $"RouteConfirm transaction submitted. Contract: { contract }.");
                            }
                            else
                            {
                                DatabaseHelper.Log(node.PublicKey, $"RouteConfirm transaction failed. Contract: { contract }.");
                            }
                        }
                    }
                    else
                    {
                        if (!IsBaseStationClose())
                        {
                            var childContract = FindNextNode(body.ContractAddress);
                            if (!string.IsNullOrEmpty(childContract))
                            {
                                var seller = contractHelper.GetSeller(node.PublicKey, node.Password, childContract);
                                body.ContractsChain.Add(childContract);
                                ServiceBusHelper.SendMessageToTopic(node, new Node() { PublicKey = seller }, baseStationNode, childContract, WhisperMessage.State.RouteConfirmed, body.ContractsChain);
                                DatabaseHelper.Log(node.PublicKey, $"RouteConfirm whisper sent to seller. Contract: { body.ContractAddress }. Seller: {seller}.");
                            }
                            else
                            {
                                DatabaseHelper.Log(node.PublicKey, $"Didn't find a contract with parent: {body.ContractAddress} which this node had created.");
                            }
                        }
                        else
                        {
                            DatabaseHelper.Log(node.PublicKey, $"Node is close to Base Station. Passing the message to Base Station.");
                            ServiceBusHelper.SendMessageToTopic(node, new Node() { PublicKey = baseStationNode.PublicKey }, baseStationNode, body.ContractAddress, WhisperMessage.State.RouteConfirmed, body.ContractsChain);
                            DatabaseHelper.Log(node.PublicKey, $"RouteConfirm whisper sent to Base Station. Contract: { body.ContractAddress }.");
                        }
                    }
                    break;

                default:
                    break;
            }

            return true;

        }

        private static string FindNextNode(string contractAddress)
        {
            string path = $@"{Environment.CurrentDirectory}\{node.PublicKey}.txt";
            if (File.Exists(path))
            {
                var addresses = File.ReadAllLines(path);
                var line = addresses.FirstOrDefault(m => m.Contains(contractAddress));
                if (line != null)
                {
                    return line.Replace(",", "").Replace(contractAddress, "");
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return string.Empty;
            }
        }

        private static bool AlreadyInvolvedInThisContractChain(string contractAddress, string parentAddress)
        {
            string path = $@"{Environment.CurrentDirectory}\{node.PublicKey}.txt";
            if (File.Exists(path))
            {
                var addresses = File.ReadAllLines(path);
                if (addresses.Any(m => m == contractAddress || m == parentAddress))
                    return true;
                else
                    return false;

            }
            else
            {
                return false;
            }
        }

        private static void SaveContractLocally(string contractAddress, string parentContract)
        {
            if (string.IsNullOrEmpty(contractAddress))
            {
                return;
            }

            string path = $@"{Environment.CurrentDirectory}\{node.PublicKey}.txt";
            if (!File.Exists(path))
            {
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine($"{contractAddress},{parentContract}");
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine($"{contractAddress},{parentContract}");
                }
            }
        }

        private static void SendContractCreatedMessageToNeighborNodes(string contractAddress, string excludeNeighborAddress)
        {
            if (string.IsNullOrEmpty(contractAddress))
            {
                DatabaseHelper.Log(node.PublicKey, "Contract was not submitted successfully.");
                return;
            }

            var neighborNodes = DatabaseHelper.GetNeighborNodes(node, CoverageArea);
            if (neighborNodes != null)
            {
                DatabaseHelper.Log(node.PublicKey, $"Found close nodes. nodes are close to this node. Neighbor nodes Count: {neighborNodes.Count}");
                foreach (var neighborNode in neighborNodes)
                {
                    if(string.IsNullOrEmpty(excludeNeighborAddress) || neighborNode.PublicKey != excludeNeighborAddress)
                    { 
                        ServiceBusHelper.SendMessageToTopic(node, neighborNode, baseStationNode, contractAddress, WhisperMessage.State.ContractCreated, null);
                    }
                }
            }
            else
            {
                DatabaseHelper.Log(node.PublicKey, "No nodes are close to this node.");
            }
        }

        private static bool IsBaseStationClose()
        {
            return Math.Sqrt(Math.Pow(Math.Abs(node.PositionX - baseStationNode.PositionX), 2) + Math.Pow(Math.Abs(node.PositionY - baseStationNode.PositionY), 2)) < CoverageArea;
        }

    }
}
