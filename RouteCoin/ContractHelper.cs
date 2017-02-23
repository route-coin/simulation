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
        private static string _abi = @"[{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteFound"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""parentContracts"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getBalance"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getState"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""abort"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[{""name"":""x"",""type"":""address""}],""name"":""toString"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteConfirmed"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""state"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""inputs"":[{""name"":""_finalDestination"",""type"":""address""},{""name"":""_contractGracePeriod"",""type"":""uint256""},{""name"":""_parentContracts"",""type"":""string""}],""payable"":false,""type"":""constructor""},{""anonymous"":false,""inputs"":[],""name"":""aborted"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeFound"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeAccepted"",""type"":""event""}]";
        private static string _byteCode = "6060604052341561000c57fe5b60405161075a38038061075a83398101604090815281516020830151918301519092015b60018054600160a060020a03338116600160a060020a0319928316179092554260045560038054928616929091169190911790556005829055805161007c906000906020840190610086565b505b505050610126565b828054600181600116156101000203166002900490600052602060002090601f016020900481019282601f106100c757805160ff19168380011785556100f4565b828001600101855582156100f4579182015b828111156100f45782518255916020019190600101906100d9565b5b50610101929150610105565b5090565b61012391905b80821115610101576000815560010161010b565b5090565b90565b610625806101356000396000f300606060405236156100725763ffffffff60e060020a600035041663047854d9811461007457806305e88584146100a857806312065fe0146101385780631865c57d1461015a57806335a063b41461018e57806356ca623e146101c25780636c88c36c1461025e578063c19d93fb14610292575bfe5b341561007c57fe5b6100846102c6565b6040518082600481111561009457fe5b60ff16815260200191505060405180910390f35b34156100b057fe5b6100b861035a565b6040805160208082528351818301528351919283929083019185019080838382156100fe575b8051825260208311156100fe57601f1990920191602091820191016100de565b505050905090810190601f16801561012a5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b341561014057fe5b6101486103e8565b60408051918252519081900360200190f35b341561016257fe5b6100846103f7565b6040518082600481111561009457fe5b60ff16815260200191505060405180910390f35b341561019657fe5b610084610401565b6040518082600481111561009457fe5b60ff16815260200191505060405180910390f35b34156101ca57fe5b6100b8600160a060020a0360043516610471565b6040805160208082528351818301528351919283929083019185019080838382156100fe575b8051825260208311156100fe57601f1990920191602091820191016100de565b505050905090810190601f16801561012a5780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b341561026657fe5b61008461052a565b6040518082600481111561009457fe5b60ff16815260200191505060405180910390f35b341561029a57fe5b6100846105cc565b6040518082600481111561009457fe5b60ff16815260200191505060405180910390f35b600080805b60065460ff1660048111156102dc57fe5b146102e657610000565b6002805473ffffffffffffffffffffffffffffffffffffffff191633600160a060020a03161790556040517f78d20fa24b6a0a3596e34219ca2fd4ce740f5d3cce342d6b1d76bd879491bf7290600090a1600680546004919060ff19166001835b021790555060065460ff1691505b5b5090565b6000805460408051602060026001851615610100026000190190941693909304601f810184900484028201840190925281815292918301828280156103e05780601f106103b5576101008083540402835291602001916103e0565b820191906000526020600020905b8154815290600101906020018083116103c357829003601f168201915b505050505081565b600160a060020a033016315b90565b60065460ff165b90565b600080805b60065460ff16600481111561041757fe5b1461042157610000565b6040517f80b62b7017bb13cf105e22749ee2a06a417ffba8c7f57b665057e0f3c2e925d990600090a1600680546003919060ff1916600183610347565b021790555060065460ff1691505b5b5090565b6104796105d5565b6104816105d5565b600060146040518059106104925750595b908082528060200260200182016040525b509150600090505b601481101561051f578060130360080260020a84600160a060020a03168115156104d157fe5b0460f860020a0282828151811015156104e657fe5b9060200101907effffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff1916908160001a9053505b6001016104ab565b8192505b5050919050565b60006004805b60065460ff16600481111561054157fe5b1461054b57610000565b6040517f17e8425f2f0f52156cb58fae3262b87ffe900164617ac332659a4b0e2d8434f590600090a1600680546002919060ff19166001835b0217905550600254604051600160a060020a039182169130163180156108fc02916000818181858888f1935050505015156105be57610000565b60065460ff1691505b5b5090565b60065460ff1681565b60408051602081019091526000815290565b604080516020810190915260008152905600a165627a7a7230582005a76c733548c21a234c67f9cea69fd3d9f5bf00f374e5c31ad1fe046bfc5d200029";
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

        public string CreateContract(string nodePublicKey, string nodePassword, HexBigInteger balance, string destinationAddress, int contractGracePeriod, string parentContracts)
        {
            // create a contract
            Task.Run(async () =>
            {
                try
                {
                    await UnlockAccount(nodePublicKey, nodePassword);

                    var ipcClient = new IpcClient(_getAddress);
                    var web3 = new Web3(ipcClient);
                    var gas = new HexBigInteger(300000);
                    var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _byteCode, nodePublicKey, gas, balance, new object[] { destinationAddress, contractGracePeriod, parentContracts });
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
               catch (Exception ex)
               {
                    DatabaseHelper.Log($"Error: {ex.Message}");
                    return string.Empty;
               }
            }).GetAwaiter().GetResult();

            return string.Empty;
        }
        
        public string RouteFound(string contractAddress, string callerAddress)
        {
            Task.Run(async () =>
            {
                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var destinationAddressRouteFoundFunction = contract.GetFunction("destinationAddressRouteFound");

                var transactionHash = await destinationAddressRouteFoundFunction.SendTransactionAsync(callerAddress);
                Console.WriteLine($"RouteFound,{transactionHash},{DateTime.UtcNow}");
                Trace.WriteLine($"RouteFound,{transactionHash},{DateTime.UtcNow}");
                var keepChecking = true;
                var maxRetry = 20;
                var retry = 0;
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
                    retry++;
                    if (retry > maxRetry)
                        keepChecking = false;
                }
                return string.Empty;

            }).GetAwaiter().GetResult();

            return string.Empty;
        }

        public string RouteConfirmed(string contractAddress, string callerAddress)
        {
            Task.Run(async () =>
            {
                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var destinationAddressRouteFoundFunction = contract.GetFunction("destinationAddressRouteConfirmed");

                var transactionHash = await destinationAddressRouteFoundFunction.SendTransactionAsync(callerAddress);

                Trace.WriteLine($"RouteConfirmed,{transactionHash},{DateTime.UtcNow}");
                var keepChecking = true;
                var maxRetry = 20;
                var retry = 0;
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
                    retry++;
                    if (retry > maxRetry)
                        keepChecking = false;
                }
                return string.Empty;
            }).GetAwaiter().GetResult();

            return string.Empty;
        }

        public string GetBalance(string contractAddress, string callerAddress)
        {
            Task.Run(async () =>
            {
                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var getBalanceFunction = contract.GetFunction("getBalance");

                var transactionHash = await getBalanceFunction.SendTransactionAsync(callerAddress);

                return transactionHash;

            }).GetAwaiter().GetResult();

            return string.Empty;
        }

        public string[] GetParentContracts(string contractAddress, string callerAddress)
        {
            Task.Run(async () =>
            {
                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var getBalanceFunction = contract.GetFunction("parentContracts");

                var transactionHash = await getBalanceFunction.SendTransactionAsync(callerAddress);

                return new string[10] { transactionHash, "", "", "", "", "", "", "", "", "" };

            }).GetAwaiter().GetResult();

            return new string[10];
        }

    }
}
