﻿using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;

namespace DatabaseRepository
{
    public class DatabaseHelper
    {
        public static Node DedicateNode()
        {
            var dbContext = new RouteCoinEntities();
            var node = dbContext.Nodes.FirstOrDefault(m => m.IsRunning == false && m.IsBaseStation == true && m.IsActive == true);
            if (node == null) // base station is running, pick another one
                node = dbContext.Nodes.FirstOrDefault(m => m.IsRunning == false && m.IsBaseStation == false && m.IsActive == true);

            if (node != null)
            {
                // update the node to be running
                node.IsRunning = true;
                dbContext.SaveChanges();
            }

            if ((bool)node.IsBaseStation)
                Console.WriteLine("Node is base station");
            else
                Console.WriteLine("Regular node");

            Console.WriteLine($"Public key: { node.PublicKey }");
            Console.WriteLine($"Position X: { node.PositionX }, Y: { node.PositionY}");

            Log(node.PublicKey, "Node dedicated.");

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
            Log(node.PublicKey, "Node released");
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
                                              m.IsActive == true &&
                                              SqlFunctions.SquareRoot(Math.Pow(Math.Abs((int)node.PositionX - (int)m.PositionX), 2) + Math.Pow(Math.Abs((int)node.PositionY - (int)m.PositionY), 2)) <= coverageArea).ToList();

            return nodes;
        }
        
        public static void Log(string nodeAddress,string message, string eventName = "", bool showInConsole = true)
        {
            var dbContext = new RouteCoinEntities();
            dbContext.Logs.Add(new Log()
            {
                Event = eventName,
                CreatedDate = DateTime.UtcNow,
                Message = message,
                NodePublicKey = nodeAddress ?? "not set"
            });
            dbContext.SaveChanges();

            if(showInConsole)
            { 
                if(!string.IsNullOrEmpty(eventName))
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine(message);

                if (!string.IsNullOrEmpty(eventName))
                    Console.ResetColor();

            }
        }

        public static string CreateContract(string nodePublicKey, Int64 balance, string destinationAddress, int contractGracePeriod, string parentContract)
        {
            throw new NotImplementedException();
        }
    }
}
