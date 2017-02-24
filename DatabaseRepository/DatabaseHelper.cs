using System;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseRepository
{
    public class DatabaseHelper
    {
        public static Node DedicateNode()
        {
            var dbContext = new RouteCoinEntities();
            var node = dbContext.Nodes.FirstOrDefault(m => m.IsRunning == false && m.IsBaseStation == true);
            if (node == null) // base station is running, pick another one
                node = dbContext.Nodes.FirstOrDefault(m => m.IsRunning == false && m.IsBaseStation == false);

            if (node != null)
            {
                // update the node to be running
                node.IsRunning = true;
                dbContext.SaveChanges();
            }

            if (node.IsBaseStation)
                Console.WriteLine("Node is base station");
            else
                Console.WriteLine("Regular node");

            Console.WriteLine($"Public key: { node.PublicKey }");
            Console.WriteLine($"Position X: { node.PositionX }, Y: { node.PositionY}");

            Log("Node dedicated.");

            return node;
            
        }

        public static Node GetNodeByPublicKey(string publicKey)
        {
            var dbContext = new RouteCoinEntities();
            return dbContext.Nodes.FirstOrDefault(m => m.PublicKey == publicKey);
        }

        public static List<Node> GetActiveNodes()
        {
            var dbContext = new RouteCoinEntities();
            return dbContext.Nodes.Where(m => m.IsActive == true).ToList();
        }

        public static void ReleaseNode(Node node)
        {
            var dbContext = new RouteCoinEntities();
            var nodeToRelease = dbContext.Nodes.FirstOrDefault(m => m.NodeId == node.NodeId);
            nodeToRelease.IsRunning = false;
            dbContext.SaveChanges();
            Log("Node released");
        }

        public static Node GetBaseStation()
        {
            var dbContext = new RouteCoinEntities();
            return dbContext.Nodes.FirstOrDefault(m => m.IsBaseStation == true);
        }

        public static List<Node> GetNeighborNodes(Node node, int coverageArea)
        {
            var dbContext = new RouteCoinEntities();
            var nodes = dbContext.Nodes.Where(m => m.IsBaseStation == false &&
                                              m.NodeId != node.NodeId &&
                                              Math.Pow(Math.Abs(node.PositionX - m.PositionX), 2) + Math.Pow(Math.Abs(node.PositionY - m.PositionY), 2) <= coverageArea).ToList();

            return nodes;
        }

        public static void Log(string message, string eventName = "")
        {
            
            var dbContext = new RouteCoinEntities();
            dbContext.Logs.Add(new Log()
            {
                Event = eventName,
                CreatedDate = DateTime.UtcNow,
                Message = message,
                NodePublicKey = "not set" // Program.node?.PublicKey ?? 
            });
            dbContext.SaveChanges();
        }
    }
}
