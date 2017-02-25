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
        private static int CoverageArea = 20; // each node covers 20 meters around it

        private static string RouteCoinSubscriptionName = "RouteCoinMessages";

        static void Main(string[] args)
        {
            baseStationNode = DatabaseHelper.GetBaseStation();

            node = DatabaseHelper.DedicateNode();

            if (node == null)
            {
                DatabaseHelper.Log("No free node to be dedicated. all nodes are running");
                Console.Write("Press any key to exit");
                Console.Read();
                return;
            }

            ServiceBusHelper.SubscribeToTopic(node.PublicKey);

            var connectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];

            var Client = SubscriptionClient.CreateFromConnectionString(connectionString, node.PublicKey, RouteCoinSubscriptionName);

            // Configure the callback options.
            OnMessageOptions options = new OnMessageOptions();
            options.AutoComplete = false;
            options.AutoRenewTimeout = TimeSpan.FromMinutes(1);

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
                    DatabaseHelper.Log($"Error:{ex.Message}");
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
            var balance = new HexBigInteger(10000);
            var contractAddress = string.Empty;

            DatabaseHelper.Log($"Processing Recieved Message: { body.Subject }");

            switch (body.Subject)
            {
                case WhisperMessage.State.CreateContract:
                    if (!IsBaseStationClose())
                    {
                        // initial contract, so all parent contract addresses will be 0x
                        var parentContracts = new string[10] { "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000" };
                        contractAddress = contractHelper.CreateContract(node.PublicKey, node.Password, balance, baseStationNode.PublicKey, ContractGracePeriod, parentContracts);
                        SendContractCreatedMessageToNeighborNodes(contractAddress, string.Empty);
                    }
                    else
                    {
                        DatabaseHelper.Log($"Base Station is close to this node. No need to create a contract. Node: { node.PublicKey }");
                    }
                    break;

                case WhisperMessage.State.ContractCreated:

                    var parents = contractHelper.GetParentContracts(node.PublicKey, node.Password, body.ContractAddress);
                    if (!IsBaseStationClose())
                    {
                        DatabaseHelper.Log($"Base station is not close to this node. Creating additional contracts. Incoming contract: {body.ContractAddress}");

                        var parentContractBalance = contractHelper.GetBalance(node.PublicKey, node.Password, body.ContractAddress);
                        if (!AlreadyInvolvedInThisContractChain(body.ContractAddress, parents))
                        {
                            AddCurrentContractToParents(parents, body.ContractAddress);
                            contractAddress = contractHelper.CreateContract(node.PublicKey, node.Password, new HexBigInteger(parentContractBalance/2), baseStationNode.PublicKey, ContractGracePeriod, parents.ToArray());
                            SaveContractLocally(contractAddress);
                            SendContractCreatedMessageToNeighborNodes(contractAddress, body.FromAddress);
                        }
                        else
                        {
                            DatabaseHelper.Log($"Already involved in this contract chain. {contractAddress}");
                        }
                    }
                    else
                    {
                        DatabaseHelper.Log($"Base station is close to this node. Set the contract and its parents to RouteFound state. Incoming contract: {body.ContractAddress}. parents: TBD ");
                        contractHelper.RouteFound(node.PublicKey, node.Password, body.ContractAddress, node.PublicKey);
                        foreach (var parent in parents)
                        {
                            if(parent != "0x0000000000000000000000000000000000000000")
                            { 
                                contractHelper.RouteFound(node.PublicKey, node.Password, parent, node.PublicKey);
                            }
                        }
                    }

                    break;

                //case "RouteFound":
                //    break;

                //case "RouteConfirmed":
                //    break;

                default:
                    break;
            }

            return true;

        }

        private static bool AlreadyInvolvedInThisContractChain(string contractAddress, List<string> parents)
        {
            string path = $@"{Environment.CurrentDirectory}\{node.PublicKey}.txt";
            if (File.Exists(path))
            {
                var addresses = File.ReadAllLines(path);
                if (addresses.Any(m => parents.Contains(m)) || addresses.Any(m => m == contractAddress))
                    return true;
                else
                    return false;

            }
            else
            {
                return false;
            }
        }

        private static void SaveContractLocally(string contractAddress)
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
                    sw.WriteLine(contractAddress);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(contractAddress);
                }
            }
        }

        private static void SendContractCreatedMessageToNeighborNodes(string contractAddress, string excludeNeighborAddress)
        {
            if (string.IsNullOrEmpty(contractAddress))
            {
                DatabaseHelper.Log("Contract was not submitted successfully.");
                return;
            }

            var neighborNodes = DatabaseHelper.GetNeighborNodes(node, CoverageArea);
            if (neighborNodes != null)
            {
                DatabaseHelper.Log($"Found close nodes. nodes are close to this node. Neighbor nodes Count: {neighborNodes.Count}");
                foreach (var neighborNode in neighborNodes)
                {
                    if(string.IsNullOrEmpty(excludeNeighborAddress) || neighborNode.PublicKey != excludeNeighborAddress)
                    { 
                        ServiceBusHelper.SendMessageToTopic(node, neighborNode, baseStationNode, contractAddress, WhisperMessage.State.ContractCreated);
                    }
                }
            }
            else
            {
                DatabaseHelper.Log("No nodes are close to this node.");
            }
        }

        private static void AddCurrentContractToParents(List<string> parents, string contractAddress)
        {
            for (int i = 0; i < parents.Count; i++)
            {
                if (parents[i] == "0x0000000000000000000000000000000000000000")
                {
                    parents[i] = contractAddress;
                    break;
                }
            }
        }

        private static bool IsBaseStationClose()
        {
            return Math.Pow(Math.Abs(node.PositionX - baseStationNode.PositionX), 2) + Math.Pow(Math.Abs(node.PositionY - baseStationNode.PositionY), 2) < CoverageArea;
        }

    }
}
