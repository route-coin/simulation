using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Configuration;
using System;

namespace RouteCoin
{
    public class ServiceBusHelper
    {
        private static string RouteCoinTopicName = "RouteCoinTopic";
        private static string RouteCoinSubscriptionName = "RouteCoinMessages";

        public static void CreateTopic()
        {
            // Create the topic if it does not exist already.
            string connectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];

            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            if (!namespaceManager.TopicExists(RouteCoinTopicName))
            {
                namespaceManager.CreateTopic(RouteCoinTopicName);
                DatabaseHelper.Log("Topic created");
            }
        }

        public static void SubscribeToTopic(string publicKey)
        {
            string connectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];

            var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);

            //var myMessagesFilter = new SqlFilter($"ToAddress = '{ publicKey }'");

            if (!namespaceManager.SubscriptionExists(RouteCoinTopicName, RouteCoinSubscriptionName))
            {
                //namespaceManager.
                //namespaceManager.CreateSubscription(RouteCoinTopicName, RouteCoinSubscriptionName, myMessagesFilter);
                namespaceManager.CreateSubscription(RouteCoinTopicName, RouteCoinSubscriptionName);
                DatabaseHelper.Log("Subscription created.");
            }
        }

        public static void ListenToMessages()
        {
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
                    // Process message from subscription.
                    Console.WriteLine("\n**High Messages**");
                    Console.WriteLine("Body: " + body.Subject);
                    // Remove message from subscription.
                    message.Complete();
                }
                catch (Exception)
                {
                    // Indicates a problem, unlock message in subscription.
                    message.Abandon();
                }
            }, options);
        }

        public static void SendMessageToTopic(Node fromNode, Node toNode, Node baseStationNode, string contractAddress, string subject)
        {
            string connectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];

            TopicClient Client = TopicClient.CreateFromConnectionString(connectionString, RouteCoinTopicName);

            var message = new WhisperMessage()
            {
                BaseStationAddress = baseStationNode.PublicKey,
                ContractAddress = contractAddress,
                FromAddress = fromNode.PublicKey,
                ToAddress = toNode.PublicKey,
                Subject = subject
            };
            DatabaseHelper.Log($"Sent service bus message. From {fromNode.PublicKey}, To: {toNode.PublicKey}, Subject: {subject}, Contract: {contractAddress}");
            Client.Send(new BrokeredMessage(message));
        }

    }
}
