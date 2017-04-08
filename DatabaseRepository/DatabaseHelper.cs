using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Threading;

namespace DatabaseRepository
{
    public class DatabaseHelper
    {
        private static Random rnd = new Random();
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
            var dbContext = new RouteCoinEntities();
            var node = dbContext.Nodes.FirstOrDefault(m => m.PublicKey == nodePublicKey);
            if (node.Balance < balance)
                return string.Empty;

            var parent = new Contract();
            parent.HupCount = 0;
            if (!string.IsNullOrEmpty(parentContract))
            {
                parent = dbContext.Contracts.FirstOrDefault(m => m.ContractAddress == parentContract);
                if(parent.HupCount >=3)
                    return string.Empty;
            }

            var contractAddress = Guid.NewGuid().ToString();
            dbContext.Contracts.Add(new Contract()
            {
                BuyerAddress = nodePublicKey,
                ContractAddress = contractAddress,
                ContractBalance = (int?)balance,
                ContractBond = (int?)balance,
                ContractStatus = "Created",
                CreatedDateTime = DateTime.Now,
                HupCount = parent.HupCount,
                ExpiresInMinutes = contractGracePeriod,
                ParentContractAdress = parentContract,
            });

            node.Balance = node.Balance - (int?)balance;

            dbContext.SaveChanges();

            Thread.Sleep(rnd.Next(5, 10));

            return contractAddress;
        }

        public static string GetSeller(string contractAddress)
        {
            var dbContext = new RouteCoinEntities();
            var contract = dbContext.Contracts.FirstOrDefault(m => m.ContractAddress == contractAddress);
            return contract.SellerAddress;
        }

        public static string GetBuyer(string contractAddress)
        {
            var dbContext = new RouteCoinEntities();
            var contract = dbContext.Contracts.FirstOrDefault(m => m.ContractAddress == contractAddress);
            return contract.BuyerAddress;
        }


        public static int? GetBalance(string contractAddress)
        {
            var dbContext = new RouteCoinEntities();
            var contract = dbContext.Contracts.FirstOrDefault(m => m.ContractAddress == contractAddress);
            return contract.ContractBalance;
        }

        public static string GetStatus(string contractAddress)
        {
            var dbContext = new RouteCoinEntities();
            var contract = dbContext.Contracts.FirstOrDefault(m => m.ContractAddress == contractAddress);
            return contract.ContractStatus;
        }

        public static string GetParentContract(string contractAddress)
        {
            var dbContext = new RouteCoinEntities();
            var contract = dbContext.Contracts.FirstOrDefault(m => m.ContractAddress == contractAddress);
            return contract.ParentContractAdress;
        }

        public static int? GetHupCount(string contractAddress)
        {
            var dbContext = new RouteCoinEntities();
            var contract = dbContext.Contracts.FirstOrDefault(m => m.ContractAddress == contractAddress);
            return contract.HupCount;
        }

        public static string RouteFound(string nodePublicKey, string contractAddress)
        {
            var dbContext = new RouteCoinEntities();
            var contract = dbContext.Contracts.FirstOrDefault(m => m.ContractAddress == contractAddress);

            var node = dbContext.Nodes.FirstOrDefault(m => m.PublicKey == nodePublicKey);
            if (node.Balance < contract.ContractBond / 5 || contract.ContractStatus != "Created")
                return string.Empty;

            contract.SellerAddress = nodePublicKey;
            contract.RouteFoundBond = contract.ContractBond / 5;
            contract.ContractBalance = contract.ContractBalance + contract.RouteFoundBond;
            contract.RouteFoundDateTime = DateTime.Now;
            contract.ContractStatus = "RouteFound";

            node.Balance = node.Balance - (contract.ContractBond / 5);

            dbContext.SaveChanges();

            Thread.Sleep(rnd.Next(3, 7));

            return nodePublicKey;

        }

        public static string RouteConfirmed(string nodePublicKey, string contractAddress)
        {
            var dbContext = new RouteCoinEntities();
            var contract = dbContext.Contracts.FirstOrDefault(m => m.ContractAddress == contractAddress);

            if (contract.ContractStatus != "RouteFound")
                return string.Empty;

            var buyerNode = dbContext.Nodes.FirstOrDefault(m => m.PublicKey == contract.BuyerAddress);
            var sellerNode = dbContext.Nodes.FirstOrDefault(m => m.PublicKey == contract.SellerAddress);

            sellerNode.Balance = sellerNode.Balance + contract.ContractBalance;
            contract.ContractBalance = 0;
            contract.RouteConfirmDateTime = DateTime.Now;
            contract.ContractStatus = "RouteConfirmed";

            dbContext.SaveChanges();

            Thread.Sleep(rnd.Next(3, 7));

            return nodePublicKey;

        }

    }
}
