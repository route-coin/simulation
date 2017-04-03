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

    public class Contract
    {
        public string Address { get; set; }
        public Node Buyer { get; set; }
        public Node Seller { get; set; }
        public int ContractBond { get; set; }
        public int RouteFoundBond { get; set; }
        public Contract ParentContract { get; set; }
        public string CreatedDate { get; set; }
        public int ExpiresInMinutes { get; set; }
        public int HubCount { get; set; }
        public ContractStatus Status { get; set; }
        public enum ContractStatus
        {
            Created,
            RouteFound,
            RouteConfirmed,
            Expired
        }

        public Contract CreateContract(int contractBond, Node buyer, int expiresInMinutes, Contract parentContract = null)
        {
            if (buyer.RouteCoins - contractBond < 0)
                return null;

            Address = Guid.NewGuid().ToString();
            Buyer = buyer;
            buyer.RouteCoins = buyer.RouteCoins - contractBond;
            ContractBond = contractBond;
            Status = ContractStatus.Created;
            ExpiresInMinutes = expiresInMinutes;
            if (parentContract == null)
                HubCount = 1;
            else
                HubCount = parentContract.HubCount + 1;

            ParentContract = parentContract;
            return this;
        }


        public Contract RouteFound(Node seller, int routeFoundBond)
        {
            if (seller.RouteCoins - routeFoundBond < 0)
                return null;

            if (Status != ContractStatus.Created)
                return null;

            Seller = seller;
            seller.RouteCoins = seller.RouteCoins - routeFoundBond;
            RouteFoundBond = routeFoundBond;
            Status = ContractStatus.RouteFound;
            return this;
        }

        public Contract RouteConfirmed()
        {
            if (Status != ContractStatus.RouteFound)
                return null;

            Seller.RouteCoins += ContractBond;
            Seller.RouteCoins += RouteFoundBond;

            Status = ContractStatus.RouteConfirmed;
            return this;
        }

    }
}
