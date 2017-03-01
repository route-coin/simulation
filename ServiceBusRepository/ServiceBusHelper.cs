using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Configuration;
using System;
using DatabaseRepository;
using System.Collections.Generic;

namespace ServiceBusRepository
{
    public class ServiceBusHelper
    {
        private static string RouteCoinSubscriptionName = "RouteCoinMessages";
        private static string ConnectionString = ConfigurationManager.AppSettings["Microsoft.ServiceBus.ConnectionString"];

        public static void CreateTopic(string publicKey)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            if (!namespaceManager.TopicExists(publicKey))
            {
                namespaceManager.CreateTopic(publicKey);
                DatabaseHelper.Log(publicKey, $"Topic created: {publicKey}");
            }
        }

        public static void SubscribeToTopic(string publicKey)
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            if (!namespaceManager.SubscriptionExists(publicKey, RouteCoinSubscriptionName))
            {
                namespaceManager.CreateSubscription(publicKey, RouteCoinSubscriptionName);
                DatabaseHelper.Log(publicKey, $"Subscription created. Publickey: {publicKey}");
            }
        }

        public static void ListenToMessages(string publicKey)
        {
            var Client = SubscriptionClient.CreateFromConnectionString(ConnectionString, publicKey, RouteCoinSubscriptionName);

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

        public static void SendMessageToTopic(Node fromNode, Node toNode, Node baseStationNode, string contractAddress, WhisperMessage.State subject, List<string> contractsChain)
        {
            TopicClient Client = TopicClient.CreateFromConnectionString(ConnectionString, toNode.PublicKey);

            var message = new WhisperMessage()
            {
                BaseStationAddress = baseStationNode.PublicKey,
                ContractAddress = contractAddress,
                FromAddress = fromNode?.PublicKey,
                ToAddress = toNode.PublicKey,
                Subject = subject,
                ContractsChain = contractsChain
            };
            DatabaseHelper.Log(fromNode?.PublicKey, $"Sent service bus message. From {fromNode?.PublicKey}, To: {toNode?.PublicKey}, Subject: {subject}, Contract: {contractAddress}");
            Client.Send(new BrokeredMessage(message));
        }

    }
}
