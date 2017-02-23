using System;
using System.Threading.Tasks;
using System.Numerics;
using System.Configuration;
using Microsoft.ServiceBus.Messaging;

namespace RouteCoin
{
    public class Program
    {
        public static Node node { get; set; }
        private static Node baseStationNode { get; set; }

        private static int ContractGracePeriod = 10;
        private static BigInteger InitialContractBalance = 1; // 
        private static int CoverageArea = 20; // each node covers 20 meters around it

        private static string RouteCoinTopicName = "RouteCoinTopic";
        private static string RouteCoinSubscriptionName = "RouteCoinMessages";

        static void Main(string[] args)
        {
            //ServiceBusHelper.CreateTopic();

            baseStationNode = DatabaseHelper.GetBaseStation();

            node = DatabaseHelper.DedicateNode();

            ServiceBusHelper.SubscribeToTopic(node.PublicKey);

            if(!node.IsBaseStation && !IsBaseStationClose())  // only create contract when it is not BS and it is not close to BS
            {
                var parentContracts = new string[10] { "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000", "0x0000000000000000000000000000000000000000" };
                CreateContract(InitialContractBalance, parentContracts);
            }

            //ServiceBusHelper.ListenToMessages();

            var connectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];

            var Client = SubscriptionClient.CreateFromConnectionString(connectionString, RouteCoinTopicName, RouteCoinSubscriptionName);

            // Configure the callback options.
            OnMessageOptions options = new OnMessageOptions();
            options.AutoComplete = false;
            options.AutoRenewTimeout = TimeSpan.FromMinutes(1);

            Client.OnMessage((message) =>
            {
                try
                {
                    var body = message.GetBody<WhisperMessage>();
                    if(body.ToAddress == node.PublicKey)
                    {
                        if(ProcessMessage(body))
                            message.Complete();
                        else
                            message.Abandon();
                    }
                    else
                    { 
                        message.Abandon();
                    }
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

        private static void CreateContract(BigInteger contractBalance, string[] parentContracts)
        {
            var balance = new Nethereum.Hex.HexTypes.HexBigInteger(contractBalance);

            var contractHelper = new ContractHelper();

            var contractAddress = contractHelper.CreateContract(node.PublicKey, node.Password, balance, baseStationNode.PublicKey, ContractGracePeriod, parentContracts);
            //var contractAddress = "0xc21f50232BBAD3485367455dB7884f138B5d7FaF";

            if (!string.IsNullOrEmpty(contractAddress))
            {
                var neighborNodes = DatabaseHelper.GetNeighborNodes(node, CoverageArea);
                if (neighborNodes != null)
                {
                    DatabaseHelper.Log($"Found close nodes. nodes are close to this node. Neighbor nodes Count: {neighborNodes.Count}");
                    foreach (var neighborNode in neighborNodes)
                    {
                        ServiceBusHelper.SendMessageToTopic(node, neighborNode, baseStationNode, contractAddress, Contract.State.ContractCreated.ToString());
                    }
                }
                else
                {
                    DatabaseHelper.Log("No nodes are close to this node.");
                }
            }
            else
            {
                DatabaseHelper.Log("Contract was not submitted successfully.");
            }
                    
        }

        private static bool ProcessMessage(WhisperMessage body)
        {
            switch (body.Subject)
            {
                case "ContractCreated":
                    var contractHelper = new ContractHelper();
                    var balance = contractHelper.GetBalance(body.ContractAddress, node.PublicKey);
                    var parents = contractHelper.GetParentContracts(body.ContractAddress, node.PublicKey);
                    break;

                case "RouteFound":
                    break;

                case "RouteConfirmed":
                    break;

                default:
                    break;
            }

            return true;

        }

        private static bool IsBaseStationClose()
        {
            return Math.Pow(Math.Abs(node.PositionX - baseStationNode.PositionX), 2) + Math.Pow(Math.Abs(node.PositionY - baseStationNode.PositionY), 2) < CoverageArea;
        }

    }
}
