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
        private static string _abi = @"[{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteFound"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getBalance"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getState"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""abort"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[{""name"":""x"",""type"":""address""}],""name"":""toString"",""outputs"":[{""name"":"""",""type"":""string""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteConfirmed"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""state"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[{""name"":"""",""type"":""uint256""}],""name"":""parentContracts"",""outputs"":[{""name"":"""",""type"":""address""}],""payable"":false,""type"":""function""},{""inputs"":[{""name"":""_finalDestination"",""type"":""address""},{""name"":""_contractGracePeriod"",""type"":""uint256""},{""name"":""_parentContracts"",""type"":""address[10]""}],""payable"":false,""type"":""constructor""},{""anonymous"":false,""inputs"":[],""name"":""aborted"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeFound"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeAccepted"",""type"":""event""}]";
        private static string _byteCode = "6060604052341561000c57fe5b604051610180806106308339810160409081528151602083015190929091015b600a8054600160a060020a03338116600160a060020a03199283161790925542600d55600c805492861692909116919091179055600e82905560005b600a8110156100be578181600a811061007d57fe5b6020020151600082600a811061008f57fe5b0160005b6101000a815481600160a060020a030219169083600160a060020a031602179055505b600101610068565b5b505050505b61055d806100d36000396000f300606060405236156100725763ffffffff60e060020a600035041663047854d9811461007457806312065fe0146100a85780631865c57d146100ca57806335a063b4146100fe57806356ca623e146101325780636c88c36c146101ce578063c19d93fb14610202578063cd5e741814610236575bfe5b341561007c57fe5b610084610265565b6040518082600481111561009457fe5b60ff16815260200191505060405180910390f35b34156100b057fe5b6100b86102f9565b60408051918252519081900360200190f35b34156100d257fe5b610084610308565b6040518082600481111561009457fe5b60ff16815260200191505060405180910390f35b341561010657fe5b610084610312565b6040518082600481111561009457fe5b60ff16815260200191505060405180910390f35b341561013a57fe5b61014e600160a060020a0360043516610382565b604080516020808252835181830152835191928392908301918501908083838215610194575b80518252602083111561019457601f199092019160209182019101610174565b505050905090810190601f1680156101c05780820380516001836020036101000a031916815260200191505b509250505060405180910390f35b34156101d657fe5b61008461043b565b6040518082600481111561009457fe5b60ff16815260200191505060405180910390f35b341561020a57fe5b6100846104dd565b6040518082600481111561009457fe5b60ff16815260200191505060405180910390f35b341561023e57fe5b6102496004356104e6565b60408051600160a060020a039092168252519081900360200190f35b600080805b600f5460ff16600481111561027b57fe5b1461028557610000565b600b805473ffffffffffffffffffffffffffffffffffffffff191633600160a060020a03161790556040517f78d20fa24b6a0a3596e34219ca2fd4ce740f5d3cce342d6b1d76bd879491bf7290600090a1600f80546004919060ff19166001835b0217905550600f5460ff1691505b5b5090565b600160a060020a033016315b90565b600f5460ff165b90565b600080805b600f5460ff16600481111561032857fe5b1461033257610000565b6040517f80b62b7017bb13cf105e22749ee2a06a417ffba8c7f57b665057e0f3c2e925d990600090a1600f80546003919060ff19166001836102e6565b0217905550600f5460ff1691505b5b5090565b61038a61050d565b61039261050d565b600060146040518059106103a35750595b908082528060200260200182016040525b509150600090505b6014811015610430578060130360080260020a84600160a060020a03168115156103e257fe5b0460f860020a0282828151811015156103f757fe5b9060200101907effffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff1916908160001a9053505b6001016103bc565b8192505b5050919050565b60006004805b600f5460ff16600481111561045257fe5b1461045c57610000565b6040517f17e8425f2f0f52156cb58fae3262b87ffe900164617ac332659a4b0e2d8434f590600090a1600f80546002919060ff19166001835b0217905550600b54604051600160a060020a039182169130163180156108fc02916000818181858888f1935050505015156104cf57610000565b600f5460ff1691505b5b5090565b600f5460ff1681565b600081600a81106104f357fe5b0160005b915054906101000a9004600160a060020a031681565b60408051602081019091526000815290565b604080516020810190915260008152905600a165627a7a723058205a35b8bd295a36399ce09cb58380d59fbdfbcfa8602956eb6fff7c7476ae7d550029";
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

        public string CreateContract(string nodePublicKey, string nodePassword, HexBigInteger balance, string destinationAddress, int contractGracePeriod, string[] parentContracts)
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
                    var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _byteCode, nodePublicKey, gas, balance, destinationAddress, contractGracePeriod, parentContracts);
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
