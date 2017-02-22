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
        private static string ContractBalance = "1";
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
                CreateContract();

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

        private static bool ProcessMessage(WhisperMessage body)
        {
            switch (body.Subject)
            {
                case "ContractCreated":

                
                default:
                    break;
            }

        }

        private static void CreateContract()
        {
            // create a contract
            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var gas = new Nethereum.Hex.HexTypes.HexBigInteger(300000);
                        var balance = new Nethereum.Hex.HexTypes.HexBigInteger(BigInteger.Parse(ContractBalance));
                        var contractGracePeriod = ContractGracePeriod;

                        var contractHelper = new ContractHelper();

                        var unlocked = await contractHelper.UnlockAccount(node.PublicKey, node.Password);
                        //var contractAddress = await contractHelper.CreateContract(node.PublicKey, gas, balance, baseStationNode.PublicKey, contractGracePeriod);

                        var contractAddress = "0xc21f50232BBAD3485367455dB7884f138B5d7FaF";
                        

                        if (!string.IsNullOrEmpty(contractAddress))
                        {
                            // transaction added to the block.
                            // broadcast the public key of the contract to the network.
                            // send messegaes to service bus
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
                    catch (Exception ex)
                    {
                        DatabaseHelper.Log($"Error: {ex.Message}");
                    }


                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                DatabaseHelper.Log($"Error: {ex.Message}");
            }
            finally
            {

            }

        }

        private static bool IsBaseStationClose()
        {
            return Math.Pow(Math.Abs(node.PositionX - baseStationNode.PositionX), 2) + Math.Pow(Math.Abs(node.PositionY - baseStationNode.PositionY), 2) < CoverageArea;
        }

    }
}
