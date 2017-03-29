using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RouteCoinCharts
{
    public class Network
    {
        List<Node> Nodes { get; set; }
        Node BaseNode { get; set; }
    }

    public class Node
    {
        public Node(string publicKey, string ipAddress, bool isBaseNode, int positionX, int positionY)
        {
            IpAddress = ipAddress;
            PublicKey = publicKey;
            IsBaseNode = isBaseNode;
            PositionX = positionX;
            PositionY = positionY;
            RouteCoins = 100;
        }
        public string IpAddress { get; set; }
        public string PublicKey { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public bool IsBaseNode { get; set; }
        public int RouteCoins { get; set; }
        public List<Node> Adjacents { get; set; }


    }

    public class NetworkTopology {
        public Node Node { get; set; }
        public DateTime Time { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
    }
}
