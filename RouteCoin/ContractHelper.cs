using Nethereum.JsonRpc.IpcClient;
using Nethereum.Web3;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Nethereum.Hex.HexTypes;

namespace RouteCoin
{
    public class ContractHelper
    {
        // Smart Contract API (json interface) and byte code
        private static string _abi = @"[{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteFound"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getState"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""abort"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteConfirmed"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""state"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""inputs"":[{""name"":""_finalDestination"",""type"":""address""},{""name"":""_contractGracePeriod"",""type"":""uint256""}],""type"":""constructor""},{""anonymous"":false,""inputs"":[],""name"":""aborted"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeFound"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeAccepted"",""type"":""event""}]";
        private static string _byteCode = "6060604081815280610273833960a090525160805160008054600160a060020a03199081166c010000000000000000000000003381028190049190911783554260035560028054909216948102049390931790925560045561020d90819061006690396000f3606060405260e060020a6000350463047854d9811461004a5780631865c57d1461006757806335a063b4146100855780636c88c36c146100a2578063c19d93fb146100c1575b610002565b3461000257610073600554600090819060ff16156100d257610002565b346100025760055460ff165b60408051918252519081900360200190f35b3461000257610073600554600090819060ff161561014457610002565b346100025761007360055460009060049060ff16811461018657610002565b346100025761007360055460ff1681565b6001805473ffffffffffffffffffffffffffffffffffffffff19166c01000000000000000000000000338102041790556040517f78d20fa24b6a0a3596e34219ca2fd4ce740f5d3cce342d6b1d76bd879491bf7290600090a16005805460ff19166004179081905560ff1691505b5090565b6040517f80b62b7017bb13cf105e22749ee2a06a417ffba8c7f57b665057e0f3c2e925d990600090a16005805460ff19166003179081905560ff169150610140565b6040517f17e8425f2f0f52156cb58fae3262b87ffe900164617ac332659a4b0e2d8434f590600090a16040516005805460ff1916600217905560015473ffffffffffffffffffffffffffffffffffffffff90811691309091163180156108fc02916000818181858888f19350505050151561020057610002565b60055460ff16915061014056";
        private static string _getAddress = "./geth.ipc";

        private static Nethereum.Hex.HexTypes.HexBigInteger _accountUnlockTime = new Nethereum.Hex.HexTypes.HexBigInteger(120);

        public async Task<bool> UnlockAccount(string buyerPublicKey, string buyerAccountPassword)
        {
            DatabaseHelper.Log($"TryToUnlockAccount,{buyerPublicKey}");

            var ipcClient = new IpcClient(_getAddress);
            var web3 = new Web3(ipcClient);

            // Unlock the caller's account with the given password
            var unlockResult = await web3.Personal.UnlockAccount.SendRequestAsync(buyerPublicKey, buyerAccountPassword, _accountUnlockTime);

            DatabaseHelper.Log($"AccountUnlocked,{unlockResult}");

            return unlockResult;
        } 

        public async Task<string> CreateContract(string nodePublicKey, string nodePassword, HexBigInteger gas, HexBigInteger balance, string destinationAddress, int contractGracePeriod)
        {

            await UnlockAccount(nodePublicKey, nodePassword);

            var ipcClient = new IpcClient(_getAddress);
            var web3 = new Web3(ipcClient);

            var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _byteCode, nodePublicKey, gas, balance, destinationAddress, contractGracePeriod);
            DatabaseHelper.Log($"ContractCreated,{transactionHash}");
            var keepChecking = true;
            var maxRetry = 20;
            var retry = 0;
            while (keepChecking)
            {
                var reciept = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                if (reciept != null)
                {
                    DatabaseHelper.Log($"ContractSubmitted,{reciept.ContractAddress}", $"ContractCreated:{reciept.ContractAddress}");
                    return reciept.ContractAddress;
                }
                // Transacion not submitted. wait 3 seconds and check again
                System.Threading.Thread.Sleep(3000);
                retry++;
                if (retry > maxRetry)
                    keepChecking = false;
            }
            return string.Empty;
        }

        public async Task<string> RouteFound(string contractAddress, string callerAddress)
        {
            var ipcClient = new IpcClient(_getAddress);
            var web3 = new Web3(ipcClient);

            var contract = web3.Eth.GetContract(_abi, contractAddress);
            var destinationAddressRouteFoundFunction = contract.GetFunction("destinationAddressRouteFound");

            var transactionHash = await destinationAddressRouteFoundFunction.SendTransactionAsync(callerAddress);
            Console.WriteLine($"RouteFound,{transactionHash},{DateTime.UtcNow}");
            Trace.WriteLine($"RouteFound,{transactionHash},{DateTime.UtcNow}");
            var keepChecking = true;
            while (keepChecking)
            {
                var reciept = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                if (reciept != null)
                {
                    Trace.WriteLine($"RouteFoundSubmitted,{reciept.ContractAddress},{DateTime.UtcNow}");
                    return reciept.ContractAddress;
                }
                // Transacion not submitted. wait 3 seconds and check again
                System.Threading.Thread.Sleep(3000);
            }
            return string.Empty;
        }

        public async Task<string> RouteConfirmed(string contractAddress, string callerAddress)
        {
            var ipcClient = new IpcClient(_getAddress);
            var web3 = new Web3(ipcClient);

            var contract = web3.Eth.GetContract(_abi, contractAddress);
            var destinationAddressRouteFoundFunction = contract.GetFunction("destinationAddressRouteConfirmed");

            var transactionHash = await destinationAddressRouteFoundFunction.SendTransactionAsync(callerAddress);

            Trace.WriteLine($"RouteConfirmed,{transactionHash},{DateTime.UtcNow}");
            var keepChecking = true;
            while (keepChecking)
            {
                var reciept = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                if (reciept != null)
                {
                    Trace.WriteLine($"RouteConfirmedSubmitted,{reciept.ContractAddress},{DateTime.UtcNow}");
                    return reciept.ContractAddress;
                }
                // Transacion not submitted. wait 3 seconds and check again
                System.Threading.Thread.Sleep(3000);
            }
            return string.Empty;
        }

    }
}
