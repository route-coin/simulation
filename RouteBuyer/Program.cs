using Nethereum.JsonRpc.IpcClient;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Common;
using System.Diagnostics;
using System.Numerics;

namespace CreateContract
{
    class Program
    {

        //private static Nethereum.Hex.HexTypes.HexBigInteger _accountUnlockTime = new Nethereum.Hex.HexTypes.HexBigInteger(120);

        static void Main(string[] args)
        {
            Trace.Listeners.Add(new TextWriterTraceListener("TextWriterOutput.log", "myListener"));
            try
            {
                Task.Run(async () =>
                {
                    if (args.Length < 5)
                    {
                        Console.WriteLine("Some arguments missing.");
                        Console.WriteLine("Expected arguments:");
                        Console.WriteLine("buyerPublicKey buyerAccountPassword destinationAddress contractPrice contractGracePeriod");
                        return;
                    }

                    var buyerPublicKey = args[0];
                    var buyerAccountPassword = args[1];
                    var destinationAddress = args[2];
                    var gas = new Nethereum.Hex.HexTypes.HexBigInteger(300000);
                    var balance = new Nethereum.Hex.HexTypes.HexBigInteger(BigInteger.Parse(args[3]));
                    var contractGracePeriod = int.Parse(args[4]);

                    var contractHelper = new ContractHelper();

                    var unlocked = await contractHelper.UnlockAccount(buyerPublicKey, buyerAccountPassword);
                    var contractAddress = await contractHelper.CreateContract(buyerPublicKey, gas, balance, destinationAddress, contractGracePeriod);

                    if(!string.IsNullOrEmpty(contractAddress))
                    {
                        Trace.WriteLine($"ContractSubmitBroadcasted,{contractAddress},{DateTime.UtcNow}");
                        // transaction added to the block.
                        // broadcast the public key of the contract to the network.
                        var udpTransceiver = new UdpTransceiver();
                        udpTransceiver.Send($"ContractSubmitted@{contractAddress}");
                    }
                    else
                    {
                        Trace.WriteLine($"ContractFailedToSubmit,{DateTime.UtcNow}");
                    }


                }).GetAwaiter().GetResult();
            }
            catch (Exception ex) {
                Trace.WriteLine($"Error: {ex.Message},{DateTime.UtcNow}");
            }
            finally {
                Trace.Flush();
            }

        }
    }
}
