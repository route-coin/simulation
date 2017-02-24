using System;
using System.Threading.Tasks;
using System.Numerics;
using System.Configuration;
using Microsoft.ServiceBus.Messaging;
using System.Collections.Generic;
using DatabaseRepository;
using ServiceBusRepository;
using Nethereum.Hex.HexTypes;

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
            //ServiceBusHelper.CreateTopic();

            baseStationNode = DatabaseHelper.GetBaseStation();

            node = DatabaseHelper.DedicateNode();

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
                    if(ProcessMessage(message))
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

            Console.Write("Press any key to exit");
            Console.Read();

            DatabaseHelper.ReleaseNode(node);
        }

        private static bool ProcessMessage(BrokeredMessage message)
        {
            var body = message.GetBody<WhisperMessage>();
            var contractHelper = new ContractHelper();
            var balance = new HexBigInteger(10000);
            var contractAddress = string.Empty;

            switch (body.Subject)
            {
                case WhisperMessage.State.CreateContract:
                    // initial contract, so all parent contract addresses will be 0x
                    var parentContracts = new string[10] { "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000" };
                    contractAddress = contractHelper.CreateContract(node.PublicKey, node.Password, balance, baseStationNode.PublicKey, ContractGracePeriod, parentContracts);
                    SendContractCreatedMessageToNeighborNodes(contractAddress);
                    break;

                case WhisperMessage.State.ContractCreated:
                    // if node is close to BS, then can confirm
                    // todo: add code to confirm
                    //else if not close to BS
                    var parentContractBalance = contractHelper.GetBalance(node.PublicKey, node.Password, body.ContractAddress);
                    var parents = contractHelper.GetParentContracts(node.PublicKey, node.Password, body.ContractAddress);
                    AddCurrentContractToParents(parents, body.ContractAddress);

                    // todo: devide parentContractBalance by 2
                    // todo: dont send to the sender node?
                    contractAddress = contractHelper.CreateContract(node.PublicKey, node.Password, new HexBigInteger(parentContractBalance), baseStationNode.PublicKey, ContractGracePeriod, parents.ToArray());
                    SendContractCreatedMessageToNeighborNodes(contractAddress);

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

        private static void SendContractCreatedMessageToNeighborNodes(string contractAddress)
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
                    ServiceBusHelper.SendMessageToTopic(node, neighborNode, baseStationNode, contractAddress, WhisperMessage.State.ContractCreated);
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
