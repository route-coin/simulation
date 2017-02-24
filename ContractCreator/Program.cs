﻿using DatabaseRepository;
using ServiceBusRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContractCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            var nodes = DatabaseHelper.GetActiveNodes();
            foreach (var node in nodes)
            {
                ServiceBusHelper.CreateTopic(node.PublicKey);
                Console.WriteLine($"Topic created/exist: {node.PublicKey}");
            }

            Console.WriteLine("All topics created.");

            Console.WriteLine("...");
            Console.WriteLine("...");

            Console.WriteLine("Press \"C + ENTER\" to send a message to create contract on a random active node");
            Console.WriteLine("Press \"C NODE_PUBLIC_KEY ENTER\" +  to send a message to create contract on that specific node");
            Console.WriteLine("Press Ctrl + C to Exit");

            var random = new Random();

            while (true)
            {
                var command = Console.ReadLine();
                if (command.StartsWith("C"))
                {
                    var commands = command.Split(' ');
                    if (commands.Length > 1) 
                    {
                        SendCreateContractMessage(commands[1]);
                    }
                    else
                    {
                        SendCreateContractMessage(random);
                    }

                }
                else
                {
                    Console.WriteLine("Press \"C + ENTER\" to send a message to create contract on a random active node");
                    Console.WriteLine("Press \"C NODE_PUBLIC_KEY ENTER\" +  to send a message to create contract on that specific node");
                }
            }

        }

        private static void SendCreateContractMessage(Random random)
        {
            // TODO: pick a node that is not close to BS

            var baseStationNode = DatabaseHelper.GetBaseStation();
            var nodes = DatabaseHelper.GetActiveNodes().Where(m=>!m.IsBaseStation).ToList();
            var node = nodes[random.Next(0, nodes.Count - 1)];
            ServiceBusHelper.SendMessageToTopic(new Node(), node, baseStationNode, null, WhisperMessage.State.CreateContract);
            Console.WriteLine($"Message sent to create contract. Node: {node.PublicKey}");
        }

        private static void SendCreateContractMessage(string publicKey)
        {
            // TODO: pick a node that is not close to BS

            var baseStationNode = DatabaseHelper.GetBaseStation();
            var nodes = DatabaseHelper.GetActiveNodes().Where(m => !m.IsBaseStation).ToList();
            var node = DatabaseHelper.GetNodeByPublicKey(publicKey);
            if (node != null)
            { 
                ServiceBusHelper.SendMessageToTopic(new Node(), node, baseStationNode, null, WhisperMessage.State.CreateContract);
                Console.WriteLine($"Message sent to create contract. Node: {publicKey}");
            }
            else
                Console.WriteLine($"Entered public key not valid:{ publicKey}");

        }
    }
}
