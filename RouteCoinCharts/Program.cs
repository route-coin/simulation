using System;
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
            var nodeCount = 21;
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
                for (int j = 0; j < nodeCount-1; j++)
                {
                    var randomBuyer = nodes[rnd.Next(1, nodeCount - 1)];

                    // node is close to base startion, so nothing to do. pick another node;
                    if (Math.Sqrt(Math.Pow(Math.Abs(randomBuyer.PositionX - nodes[0].PositionX), 2) + Math.Pow(Math.Abs(randomBuyer.PositionY - nodes[0].PositionY), 2)) <= 10)
                        continue;
                    // get nodes betweem 10 meter of the random buyer, in this time "now" in topology
                    var closeNodes = topologies.Where(m => m.Time == now)
                                                .Where(m => Math.Sqrt(Math.Pow(Math.Abs(m.PositionX - randomBuyer.PositionX), 2) + Math.Pow(Math.Abs(m.PositionY - randomBuyer.PositionY), 2)) <= 10)
                                                //.Where(m => Math.Sqrt(Math.Pow(Math.Abs(m.PositionX - nodes[0].PositionX), 2) + Math.Pow(Math.Abs(m.PositionY - nodes[0].PositionY), 2)) <= 20)
                                                .Where(m => m.Node.PublicKey != nodes[0].PublicKey && m.Node.PublicKey != randomBuyer.PublicKey).ToList();

                    var contractPublicKey = GenerateNewContractPublicKey();

                    log(now, randomBuyer.PublicKey, randomBuyer.IpAddress, Events.ContractCreated.ToString(), contractPublicKey);

                    if (closeNodes.Count() > 0)
                    {
                        log(now, randomBuyer.PublicKey, randomBuyer.IpAddress, Events.ContractCreated.ToString(), contractPublicKey);
                        foreach (var node in closeNodes)
                        {
                            log(now, node.Node.PublicKey, node.Node.IpAddress, Events.ContractRead.ToString(), contractPublicKey);
                        }

                        // get nodes betweem 10 meter of the random buyer, in this time "now" in topology
                        var closeNodesToBs = closeNodes.Where(m => m.Time == now)
                                                    .Where(m => Math.Sqrt(Math.Pow(Math.Abs(m.PositionX - 250), 2) + Math.Pow(Math.Abs(m.PositionY - 250), 2)) <= 10).ToList();
                                                    //.Where(m => m.Node.PublicKey != nodes[0].PublicKey && m.Node.PublicKey != randomBuyer.PublicKey).ToList();

                        if(closeNodesToBs.Count() > 0)
                        { 
                            var randonSeller = closeNodesToBs[rnd.Next(0, closeNodesToBs.Count() - 1)];

                            log(now.AddSeconds(rnd.Next(10, 16)), randonSeller.Node.PublicKey, randonSeller.Node.IpAddress, Events.RouteFound.ToString(), contractPublicKey);

                            log(now.AddSeconds(rnd.Next(20,32)), nodes[0].PublicKey, nodes[0].IpAddress, Events.RouteConfirmed.ToString(), contractPublicKey);
                        }
                    }
                }

            }
                //    for (int i = 0; i < contractCount; i++)
                //    {
                //    var randomStart = new Random().Next(5, 10);
                //    var contractTime = DateTime.Now.AddMinutes(randomStart);
                //    var contractPublicKey = GenerateNewContractPublicKey();
                //    var nodesHashset = new HashSet<string>();
                //    var randomSourceNode = new Random().Next(1, nodeCount - 1);
                //    var sourceNode = nodes[randomSourceNode];
                //    nodesHashset.Add(sourceNode.PublicKey);

                //    log(contractTime, sourceNode.PublicKey, sourceNode.IpAddress, Events.ContractCreated.ToString(), contractPublicKey, sourceNode.PositionX, sourceNode.PositionY);
                //    var randomChildCount = new Random().Next(3, 5);

                //    for (int j = 0; j < randomChildCount; j++)
                //    {
                //        var rndChild = new Random().Next(1, nodeCount - 1);
                //        var child = nodes[rndChild];
                //        if (nodesHashset.Add(child.PublicKey))
                //        {
                //            log(contractTime, child.PublicKey, child.IpAddress, Events.ContractRead.ToString(), contractPublicKey, child.PositionX, child.PositionY);
                //            children.Add(child);
                //        }
                //    }

                //    if (children.Count > 0)
                //    { 
                //        var randomChild = children[new Random().Next(0, children.Count - 1)];
                //        var random1 = new Random().Next(10, 14);
                //        log(contractTime.AddSeconds(random1), randomChild.PublicKey, randomChild.IpAddress, Events.RouteFound.ToString(), contractPublicKey, randomChild.PositionX, randomChild.PositionY);
                //        if (randomChild.PositionX + 10 < 500)
                //            randomChild.PositionX += 10;  
                //        else
                //            randomChild.PositionX -= 10; 

                //        if (randomChild.PositionY + 10 < 500)
                //            randomChild.PositionY += 10;  
                //        else
                //            randomChild.PositionY -= 10; 

                //        var random2 = new Random().Next(10, 14);
                //        log(contractTime.AddSeconds(random2), contractDestination.PublicKey, contractDestination.IpAddress, Events.RouteConfirmed.ToString(), contractPublicKey, contractDestination.PositionX, contractDestination.PositionY);
                //    }

                //    if (sourceNode.PositionX + 10 < 500)
                //        sourceNode.PositionX += 10;
                //    else
                //        sourceNode.PositionX -= 10;

                //    if (sourceNode.PositionY + 10 < 500)
                //        sourceNode.PositionY += 10;
                //    else
                //        sourceNode.PositionY -= 10;

                //    children.Clear();

                //    nodesHashset.Clear();
                //}

        }

        private static List<NetworkTopology> GenerateTopoligies(int simulationPeriod, DateTime startTime, Random rnd)
        {
            var networkTopology = new List<NetworkTopology>();
            for (int i = 0; i < simulationPeriod; i++)
            {
                //var s = $"{startTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} {nodes[0].PositionX} {nodes[0].PositionY}";
                var s = $"{nodes[0].PositionX} {nodes[0].PositionY}";
                File.AppendAllLines($"{Environment.CurrentDirectory}\\moves.txt", new List<string>() { s });
                networkTopology.Add(new NetworkTopology() { Node = nodes[0], PositionX = nodes[0].PositionX, PositionY = nodes[0].PositionY, Time = startTime });
                for (int j = 1; j < nodes.Count(); j++)
                {
                    MoveNode(nodes[j], rnd);
                    networkTopology.Add(new NetworkTopology() { Node = nodes[j], PositionX = nodes[j].PositionX, PositionY = nodes[j].PositionY, Time = startTime });

                    //s = $"{startTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} {nodes[j].PositionX} {nodes[j].PositionY}";
                    s = $"{nodes[j].PositionX} {nodes[j].PositionY}";
                    File.AppendAllLines($"{Environment.CurrentDirectory}\\moves.txt", new List<string>() { s });
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
            var XorY = rnd.Next(1,2);
            var PlusOrMinus = rnd.Next(1, 2);
            if (XorY == 1)
            {
                if(PlusOrMinus == 1)
                    if(node.PositionX < 500)
                        node.PositionX += 1;
                    else
                        node.PositionX -= 1;
                else
                    if(node.PositionX > 0)
                        node.PositionX -= 1;
                    else
                        node.PositionX += 1;

            }
            else
            {
                if (PlusOrMinus == 1)
                    if(node.PositionY < 500)
                        node.PositionY += 1;
                    else
                        node.PositionY -= 1;
                else
                    if(node.PositionY > 0)
                        node.PositionY -= 1;
                    else
                        node.PositionY += 1;
            }
        }

        private static void GenerateNewNodes(int nodeCount, Random rnd)
        {
            for (int i = 1; i < nodeCount; i++)
            {
                var publicKey = $"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}";
                var ipAddress = RandomIpGenerator.GetRandomIp(rnd);
                var randomX = rnd.Next(1, 499);
                var randomY = rnd.Next(1, 499);
                var node = new Node(publicKey, ipAddress, false, randomX, randomY);
                nodes[i] = node;
                //var s = $"{publicKey} {randomX} {randomY}";
                //File.AppendAllLines($"{Environment.CurrentDirectory}\\positions.txt", new List<string>() { s });
            }
        }

        private static string GenerateNewContractPublicKey()
        {
            return GenerateNewHexAddress();
        }

        private static Node GenerateNewDestinationNode(Random rnd)
        {
            var publicKey = $"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}";
            var ipAddress = RandomIpGenerator.GetRandomIp(rnd);
            var node = new Node(publicKey, ipAddress, true, 250, 250);
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
            File.AppendAllLines($"{Environment.CurrentDirectory}\\events.txt" , new List<string>() { s });
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
