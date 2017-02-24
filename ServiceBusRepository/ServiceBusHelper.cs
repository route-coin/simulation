using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Configuration;
using System;
using DatabaseRepository;

namespace ServiceBusRepository
{
    public class ServiceBusHelper
    {
        private static string RouteCoinTopicName = "RouteCoinTopic";
        private static string RouteCoinSubscriptionName = "RouteCoinMessages";
        private static string ConnectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];
        public static void CreateTopic()
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            if (!namespaceManager.TopicExists(RouteCoinTopicName))
            {
                namespaceManager.CreateTopic(RouteCoinTopicName);
                DatabaseHelper.Log("Topic created");
            }
        }

        public static void CreateTopic(string publicKey)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            if (!namespaceManager.TopicExists(publicKey))
            {
                namespaceManager.CreateTopic(publicKey);
                DatabaseHelper.Log($"Topic created: {publicKey}");
            }
        }

        public static void SubscribeToTopic(string publicKey)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

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
            var Client = SubscriptionClient.CreateFromConnectionString(ConnectionString, RouteCoinTopicName, RouteCoinSubscriptionName);

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
            TopicClient Client = TopicClient.CreateFromConnectionString(ConnectionString, RouteCoinTopicName);

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
