using DatabaseRepository;
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

            }
            // Create one topic for each active node in the database
            // Each node when they start will subsribe to a topic that matches their public key 

            // pick random nodes and post CreateContact messages to the related topic


        }
    }
}
