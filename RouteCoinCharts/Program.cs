﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteCoinCharts
{
    class Program
    {
        private static Node[] nodes;
        private enum Events
        {
            ContractCreated,
            ContractRead,
            RouteFound,
            RouteConfirmed
        }

        static void Main(string[] args)
        {
            var nodeCountToPick = 5;
            var nodeCount = 25;
            //var contractCount = 1000;
            var simulationPeriod = 500;
            nodes = new Node[nodeCount];
            var rnd = new Random();
            var contractDestination = GenerateNewDestinationNode(rnd);
            var children = new List<Node>();

            GenerateNewNodes(nodeCount, rnd);
            var startTime = DateTime.Now;

            PrintNodes(nodes);

            var topologies = GenerateTopoligies(simulationPeriod, startTime, rnd);

            for (int i = 0; i < simulationPeriod; i++)
            {
                var now = startTime.AddSeconds(i);

                // try to create 3 contract in each second
                for (int j = 0; j < nodeCountToPick - 1; j++)
                {
                    var buyer = nodes[rnd.Next(1, nodeCount - 1)];

                    // node is close to base startion, so nothing to do. pick another node;
                    if (IsCloseToBs(buyer, now))
                        continue;

                    buyer.RouteCoins -= 1;
                    if (buyer.RouteCoins > 0)
                        log(now, buyer.PublicKey, buyer.IpAddress, Events.ContractCreated.ToString(), GenerateNewContractPublicKey());
                    else
                        continue;

                    var closeNodes = GetNeighbors(topologies, buyer, now);

                    now = now.AddSeconds(1);

                    foreach (Node node1 in closeNodes)
                    {
                        if (!IsCloseToBs(node1, now))
                        {
                            node1.RouteCoins -= 1;
                            if (node1.RouteCoins > 0)
                                log(now, node1.PublicKey, node1.IpAddress, Events.ContractCreated.ToString(), GenerateNewContractPublicKey());
                        }
                    }

                    now = now.AddSeconds(1);

                    foreach (Node node in closeNodes)
                    {
                        if(node.RouteCoins > 0)
                        { 
                            var closeNodes1 = GetNeighbors(topologies, node, now);

                            foreach (Node node1 in closeNodes1)
                            {
                                if (!IsCloseToBs(node1, now))
                                {
                                    node1.RouteCoins -= 1;
                                    if (node1.RouteCoins > 0)
                                        log(now, node1.PublicKey, node1.IpAddress, Events.ContractCreated.ToString(), GenerateNewContractPublicKey());
                                }
                            }
                        }

                    }

                    now = now.AddSeconds(-2);

                }

            }

        }

        private static bool IsCloseToBs(Node node, DateTime now)
        {
            return Math.Sqrt(Math.Pow(Math.Abs(node.PositionX - nodes[0].PositionX), 2) + Math.Pow(Math.Abs(node.PositionY - nodes[0].PositionY), 2)) <= 50;
        }

        private static List<Node> GetNeighbors(List<NetworkTopology> topologies, Node node, DateTime now)
        {
            var topo = topologies.Where(m => m.Time == now)
                      .Where(m => Math.Sqrt(Math.Pow(Math.Abs(m.PositionX - node.PositionX), 2) + Math.Pow(Math.Abs(m.PositionY - node.PositionY), 2)) <= 50)
                      //.Where(m => Math.Sqrt(Math.Pow(Math.Abs(m.PositionX - nodes[0].PositionX), 2) + Math.Pow(Math.Abs(m.PositionY - nodes[0].PositionY), 2)) <= 20)
                      .Where(m => m.Node.PublicKey != nodes[0].PublicKey && m.Node.PublicKey != node.PublicKey);
            return topo.Select(m => m.Node).ToList();
        }

        private static List<NetworkTopology> GenerateTopoligies(int simulationPeriod, DateTime startTime, Random rnd)
        {
            var networkTopology = new List<NetworkTopology>();
            for (int i = 0; i < simulationPeriod; i++)
            {
                var s = $"{nodes[0].PositionX} {nodes[0].PositionY}";
                //File.AppendAllLines($"{Environment.CurrentDirectory}\\moves.txt", new List<string>() { s });
                networkTopology.Add(new NetworkTopology() { Node = nodes[0], PositionX = nodes[0].PositionX, PositionY = nodes[0].PositionY, Time = startTime });
                for (int j = 1; j < nodes.Count(); j++)
                {
                    MoveNode(nodes[j], rnd);
                    networkTopology.Add(new NetworkTopology() { Node = nodes[j], PositionX = nodes[j].PositionX, PositionY = nodes[j].PositionY, Time = startTime });

                    s = $"{nodes[j].PositionX} {nodes[j].PositionY}";
                    //File.AppendAllLines($"{Environment.CurrentDirectory}\\moves.txt", new List<string>() { s });
                }
                startTime = startTime.AddSeconds(1);
            }
            return networkTopology;
        }

        private static void PrintNodes(Node[] nodes)
        {
            for (int j = 0; j < nodes.Count(); j++)
            {
                var s = $"{nodes[j].PublicKey}";
                File.AppendAllLines($"{Environment.CurrentDirectory}\\nodes.txt", new List<string>() { s });
            }
        }

        private static void MoveNode(Node node, Random rnd)
        {
            //todo: make sure there is no other node with this X;
            var XorY = rnd.Next(1, 2);
            var PlusOrMinus = rnd.Next(1, 2);
            if (XorY == 1)
            {
                if (PlusOrMinus == 1)
                    if (node.PositionX < 500)
                        node.PositionX += 1;
                    else
                        node.PositionX -= 1;
                else
                    if (node.PositionX > 0)
                    node.PositionX -= 1;
                else
                    node.PositionX += 1;

            }
            else
            {
                if (PlusOrMinus == 1)
                    if (node.PositionY < 500)
                        node.PositionY += 1;
                    else
                        node.PositionY -= 1;
                else
                    if (node.PositionY > 0)
                    node.PositionY -= 1;
                else
                    node.PositionY += 1;
            }
        }

        private static void GenerateNewNodes(int nodeCount, Random rnd)
        {
            nodes[1] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 50);
            nodes[2] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 50);
            nodes[3] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 50);
            nodes[4] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 50);
            nodes[5] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 50);

            nodes[6] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 85);
            nodes[7] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 85);
            nodes[8] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 85);
            nodes[9] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 85);
            nodes[10] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 85);

            nodes[11] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 120);
            nodes[12] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 120);
            nodes[13] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 120);
            nodes[14] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 120);

            nodes[15] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 155);
            nodes[16] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 155);
            nodes[17] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 155);
            nodes[18] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 155);
            nodes[19] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 155);

            nodes[20] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 190);
            nodes[21] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 190);
            nodes[22] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 190);
            nodes[23] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 190);
            nodes[24] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 190);

            //for (int i = 1; i < nodeCount; i++)
            //{
            //    var publicKey = $"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}";
            //    var ipAddress = RandomIpGenerator.GetRandomIp(rnd);
            //    var randomX = rnd.Next(1, 499);
            //    var randomY = rnd.Next(1, 499);
            //    var node = new Node(publicKey, ipAddress, false, randomX, randomY);
            //    nodes[i] = node;
            //    //var s = $"{publicKey} {randomX} {randomY}";
            //    //File.AppendAllLines($"{Environment.CurrentDirectory}\\positions.txt", new List<string>() { s });
            //}

        }

        private static string GenerateNewContractPublicKey()
        {
            return GenerateNewHexAddress();
        }

        private static Node GenerateNewDestinationNode(Random rnd)
        {
            var publicKey = $"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}";
            var ipAddress = RandomIpGenerator.GetRandomIp(rnd);
            var node = new Node(publicKey, ipAddress, true, 120, 120);
            nodes[0] = node;
            //var s = $"{publicKey} {250} {250}";
            //File.AppendAllLines($"{Environment.CurrentDirectory}\\positions.txt", new List<string>() { s });

            return node;
        }

        private static string GenerateNewHexAddress()
        {
            return $"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}";
        }

        private static void log(DateTime timeStamp, string publicKey, string ipAddress, string eventName, string contractAddress)
        {
            //Current
            var s = $"{timeStamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} {publicKey} {ipAddress} {eventName} {contractAddress}";
            File.AppendAllLines($"{Environment.CurrentDirectory}\\events.txt", new List<string>() { s });
        }

    }

    public static class RandomIpGenerator
    {
        //private static Random _random = new Random();
        public static string GetRandomIp(Random _random)
        {
            return string.Format("{0}.{1}.{2}.{3}", _random.Next(0, 255), _random.Next(0, 255), _random.Next(0, 255), _random.Next(0, 255));
        }
    }
}