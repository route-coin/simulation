using EthereumRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContractViewer
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Enter a valid Account's Public Key:");
            var publicKey = Console.ReadLine();
            Console.WriteLine("Enter a valid Account's Password:");
            var password = Console.ReadLine();

            Console.WriteLine("Enter Contract Public Key to see all the details.");
            Console.WriteLine("Press Ctrl + C to Exit.");

            var random = new Random();

            while (true)
            {
                var contractAddress = Console.ReadLine();
                var contractHelper = new ContractHelper();
                Console.WriteLine($"ParentContract: {contractHelper.GetParentContract(publicKey, password, contractAddress)}");
                Console.WriteLine($"Balance: {contractHelper.GetBalance(publicKey, password, contractAddress)}");
                Console.WriteLine($"Buyer: {contractHelper.GetBuyer(publicKey, password, contractAddress)}");
                Console.WriteLine($"Seller: {contractHelper.GetSeller(publicKey, password, contractAddress)}");
                Console.WriteLine($"State: {contractHelper.GetState(publicKey, password, contractAddress)}");


            }
        }
    }
}
