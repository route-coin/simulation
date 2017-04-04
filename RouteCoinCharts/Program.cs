using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace RouteCoinCharts
{
    class Program
    {
        private static Node[] nodes;
        private enum Events
        {
            ContractCreated,
            ContractCreated2,
            ContractRead,
            RouteFound,
            RouteConfirmed
        }

        static void Main(string[] args)
        {
            var nodeCount = 25;
            var simulationPeriod = 1000;
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

                var buyer = nodes[rnd.Next(1, nodeCount - 1)];

                // node is close to base startion, so nothing to do. pick another node;
                if (IsCloseToBs(buyer, now))
                     continue;

                var contract = new Contract();

                now = now.AddSeconds(rnd.Next(12, 16));

                contract = contract.CreateContract(100, buyer, 100, null);

                if (contract == null) // not enough route coin or some other error, take another node.
                    continue;

                var closeNodes = GetNeighbors(topologies, buyer, now);

                now = now.AddSeconds(rnd.Next(12, 16));

                foreach (Node node1 in closeNodes)
                {
                    if (!IsCloseToBs(node1, now))
                    {
                        if (contract.Status == Contract.ContractStatus.Created)
                        {
                            var c1 = new Contract();
                            c1 = c1.CreateContract(contract.ContractBond / 2, node1, 100, contract);
                            if (c1 == null)
                                continue;

                            now = now.AddSeconds(rnd.Next(12, 16));

                            var closeNodes1 = GetNeighbors(topologies, node1, now);

                            foreach (Node node2 in closeNodes1)
                            {
                                if (!IsCloseToBs(node2, now))
                                {
                                    var c2 = new Contract();
                                    c2 = c2.CreateContract(c1.ContractBond / 2, node2, 100, c1);
                                    if (c2 == null)
                                        continue;

                                    now = now.AddSeconds(rnd.Next(12, 16));

                                    var closeNodes2 = GetNeighbors(topologies, node2, now);

                                    foreach (Node node3 in closeNodes2)
                                    {
                                        if (!IsCloseToBs(node3, now))
                                        {
                                            var c3 = new Contract();
                                            c3 = c3.CreateContract(c2.ContractBond / 2, node3, 100, c2);
                                            if (c3 == null)
                                                continue;

                                            now = now.AddSeconds(rnd.Next(12, 16));

                                            var closeNodes3 = GetNeighbors(topologies, node3, now);

                                            foreach (Node node4 in closeNodes3)
                                            {
                                                if (!IsCloseToBs(node4, now))
                                                {
                                                    var c4 = new Contract();
                                                    c4 = c4.CreateContract(c3.ContractBond / 2, node4, 100, c3);
                                                    if (c3 == null)
                                                        continue;
                                                }
                                                else
                                                {
                                                    now = now.AddSeconds(rnd.Next(12, 16));
                                                    c3.RouteFound(node1, c3.ContractBond / 5);
                                                }
                                            }

                                        }
                                        else
                                        {
                                            now = now.AddSeconds(rnd.Next(12, 16));
                                            c2.RouteFound(node1, c2.ContractBond / 5);

                                        }
                                    }

                                }
                                else
                                {
                                    now = now.AddSeconds(rnd.Next(12, 16));
                                    c1.RouteFound(node1, c1.ContractBond / 5);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (contract.Status == Contract.ContractStatus.Created)
                        {
                            now = now.AddSeconds(rnd.Next(12, 16));
                            contract.RouteFound(node1, contract.ContractBond / 5);
                        }
                    }

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
            var result = topo.Select(m => m.Node).ToList();
            result.Shuffle();
            return result;
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
            nodes[6] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 225, 50);
            nodes[7] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 260, 50);
            nodes[8] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 295, 50);
            nodes[9] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 330, 50);

            nodes[10] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 85);
            nodes[11] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 85);
            nodes[12] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 85);
            nodes[13] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 85);
            nodes[14] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 85);
            nodes[15] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 225, 85);
            nodes[16] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 260, 85);
            nodes[17] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 295, 85);
            nodes[18] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 330, 85);

            nodes[19] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 120);
            nodes[20] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 120);
            nodes[21] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 120);
            nodes[22] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 120);
            nodes[23] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 120);
            nodes[24] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 225, 120);
            nodes[25] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 260, 120);
            nodes[26] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 295, 120);
            nodes[27] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 330, 120);

            nodes[28] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 155);
            nodes[29] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 155);
            nodes[30] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 155);
            nodes[31] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 155);
            nodes[32] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 155);
            nodes[33] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 225, 155);
            nodes[34] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 260, 155);
            nodes[35] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 295, 155);
            nodes[36] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 330, 155);

            nodes[37] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 190);
            nodes[38] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 190);
            nodes[39] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 190);
            nodes[40] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 190);

            // Base Station
            nodes[41] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 190);

            nodes[42] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 225, 190);
            nodes[43] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 260, 190);
            nodes[44] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 295, 190);
            nodes[45] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 330, 190);

            nodes[46] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 225);
            nodes[47] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 225);
            nodes[48] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 225);
            nodes[49] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 225);
            nodes[50] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 225);
            nodes[51] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 225, 225);
            nodes[52] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 260, 225);
            nodes[53] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 295, 225);
            nodes[54] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 330, 225);

            nodes[55] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 260);
            nodes[56] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 260);
            nodes[57] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 260);
            nodes[58] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 260);
            nodes[59] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 260);
            nodes[60] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 225, 260);
            nodes[61] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 260, 260);
            nodes[62] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 295, 260);
            nodes[63] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 330, 260);

            nodes[64] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 295);
            nodes[65] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 295);
            nodes[66] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 295);
            nodes[67] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 295);
            nodes[68] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 295);
            nodes[69] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 225, 295);
            nodes[70] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 260, 295);
            nodes[71] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 295, 295);
            nodes[72] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 330, 295);

            nodes[73] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 50, 330);
            nodes[74] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 85, 330);
            nodes[75] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 120, 330);
            nodes[76] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 155, 330);
            nodes[77] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 190, 330);
            nodes[78] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 225, 330);
            nodes[79] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 260, 330);
            nodes[80] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 295, 330);
            nodes[81] = new Node($"0X{Guid.NewGuid().ToString().Replace("-", "").PadLeft(40, '0')}", RandomIpGenerator.GetRandomIp(rnd), false, 330, 330);

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
            var s = $"{timeStamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)},{publicKey},{ipAddress},{eventName},{contractAddress}";
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

    static class MyExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }


}
