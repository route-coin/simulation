using Nethereum.JsonRpc.IpcClient;
using Nethereum.Web3;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using System.Collections.Generic;
using DatabaseRepository;

namespace EthereumRepository
{
    public class ContractHelper
    {
        // Smart Contract API (json interface) and byte code
        private static string _abi = @"[{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteFound"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getBalance"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getState"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""abort"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getHupCount"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteConfirmed"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":true,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getParentContract"",""outputs"":[{""name"":"""",""type"":""address""}],""payable"":false,""type"":""function""},{""inputs"":[{""name"":""_finalDestination"",""type"":""address""},{""name"":""_contractGracePeriod"",""type"":""uint256""},{""name"":""_parentContract"",""type"":""address""}],""payable"":true,""type"":""constructor""},{""anonymous"":false,""inputs"":[],""name"":""aborted"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeFound"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeAccepted"",""type"":""event""}]";
        private static string _byteCode = "60606040526040516060806104f98339810160409081528151602083015191909201515b60028054600160a060020a03338116600160a060020a0319928316179092554260055560048054868416908316179055600684905560008054928416929091168217815590156101005781905080600160a060020a031663436565b16000604051602001526040518163ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401809050602060405180830381600087803b15156100cf57fe5b60325a03f115156100dc57fe5b505060405151600190810190819055600590111590506100fb57610000565b610106565b60006001555b5b505050505b6103de8061011b6000396000f300606060405236156100675763ffffffff60e060020a600035041663047854d9811461006957806312065fe01461009d5780631865c57d146100bf57806335a063b4146100f3578063436565b1146101275780636c88c36c14610149578063f117771214610175575bfe5b341561007157fe5b6100796101a1565b6040518082600481111561008957fe5b60ff16815260200191505060405180910390f35b34156100a557fe5b6100ad610235565b60408051918252519081900360200190f35b34156100c757fe5b610079610244565b6040518082600481111561008957fe5b60ff16815260200191505060405180910390f35b34156100fb57fe5b61007961024e565b6040518082600481111561008957fe5b60ff16815260200191505060405180910390f35b341561012f57fe5b6100ad6102dc565b60408051918252519081900360200190f35b6100796102e3565b6040518082600481111561008957fe5b60ff16815260200191505060405180910390f35b341561017d57fe5b6101856103a2565b60408051600160a060020a039092168252519081900360200190f35b600080805b60075460ff1660048111156101b757fe5b146101c157610000565b6003805473ffffffffffffffffffffffffffffffffffffffff191633600160a060020a03161790556040517f78d20fa24b6a0a3596e34219ca2fd4ce740f5d3cce342d6b1d76bd879491bf7290600090a1600780546004919060ff19166001835b021790555060075460ff1691505b5b5090565b600160a060020a033016315b90565b60075460ff165b90565b60025460009033600160a060020a0390811691161461026c57610000565b6000805b60075460ff16600481111561028157fe5b1461028b57610000565b6040517f80b62b7017bb13cf105e22749ee2a06a417ffba8c7f57b665057e0f3c2e925d990600090a1600780546003919060ff1916600183610222565b021790555060075460ff1691505b5b505b90565b6001545b90565b60045460009033600160a060020a0390811691161461030157610000565b6004805b60075460ff16600481111561031657fe5b1461032057610000565b6040517f17e8425f2f0f52156cb58fae3262b87ffe900164617ac332659a4b0e2d8434f590600090a1600780546002919060ff19166001835b0217905550600354604051600160a060020a039182169130163180156108fc02916000818181858888f19350505050151561039357610000565b60075460ff1691505b5b505b90565b600054600160a060020a03165b905600a165627a7a72305820ad2b69b89253a1849327d1b64705b88ddf3551c19d8b5a902e045d03e420e5140029";
        private static string _getAddress = "./geth.ipc";
        private static int _maxRetry = 20;
        private static int _sleepBetweenRetry = 15000;
        private static HexBigInteger _accountUnlockTime = new HexBigInteger(600);

        public async Task<bool> UnlockAccount(string publicKey, string password)
        {
            var ipcClient = new IpcClient(_getAddress);
            var web3 = new Web3(ipcClient);

            // Unlock the caller's account with the given password
            var unlockResult = await web3.Personal.UnlockAccount.SendRequestAsync(publicKey, password, _accountUnlockTime);

            //DatabaseHelper.Log($"Account unlocked: {publicKey}");

            return unlockResult;
        } 

        public string CreateContract(string nodePublicKey, string nodePassword, HexBigInteger balance, string destinationAddress, int contractGracePeriod, string parentContract)
        {
            var contractAddress = string.Empty;
            // create a contract
            Task.Run(async () =>
            {
                try
                {

                    await UnlockAccount(nodePublicKey, nodePassword);

                    var ipcClient = new IpcClient(_getAddress);
                    var web3 = new Web3(ipcClient);
                    var gas = new HexBigInteger(200000);
                    var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _byteCode, nodePublicKey, new HexBigInteger(900000), balance, new object[] { destinationAddress, contractGracePeriod, parentContract });
                    DatabaseHelper.Log($"Contract transaction submitted. trx:{transactionHash}");
                    var keepChecking = true;
                    var retry = 0;
                    while (keepChecking)
                    {
                        var reciept = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                        if (reciept != null)
                        {
                            DatabaseHelper.Log($"Contract submitted. Contract PublicKey:{reciept.ContractAddress}", $"ContractCreated,{reciept.ContractAddress},{DateTime.UtcNow}");
                            contractAddress = reciept.ContractAddress;
                            return contractAddress;
                        }
                        // Transacion not submitted. wait 3 seconds and check again
                        System.Threading.Thread.Sleep(_sleepBetweenRetry);
                        retry++;
                        if (retry > _maxRetry)
                            keepChecking = false;
                    }
                    return contractAddress;
               }
               catch (Exception ex)
               {
                    DatabaseHelper.Log($"Error: {ex.Message}");
                    return contractAddress;
               }
            }).GetAwaiter().GetResult();

            return contractAddress;
        }
        
        public string RouteFound(string nodePublicKey, string nodePassword, string contractAddress, string callerAddress)
        {
            Task.Run(async () =>
            {

                await UnlockAccount(nodePublicKey, nodePassword);

                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var destinationAddressRouteFoundFunction = contract.GetFunction("destinationAddressRouteFound");

                var transactionHash = await destinationAddressRouteFoundFunction.SendTransactionAsync(callerAddress);
                DatabaseHelper.Log($"Contract transaction submitted. trx: {transactionHash}");
                var keepChecking = true;
                var retry = 0;
                while (keepChecking)
                {
                    var reciept = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                    if (reciept != null)
                    {
                        DatabaseHelper.Log($"RouteFound submitted. Contract PublicKey:{reciept.ContractAddress}", $"RouteFound,{reciept.ContractAddress},{DateTime.UtcNow}");
                        return reciept.ContractAddress;
                    }
                    // Transacion not submitted. wait 3 seconds and check again
                    System.Threading.Thread.Sleep(_sleepBetweenRetry);
                    retry++;
                    if (retry > _maxRetry)
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

                DatabaseHelper.Log($"RouteConfirmed,{transactionHash},{DateTime.UtcNow}");
                var keepChecking = true;
                var retry = 0;
                while (keepChecking)
                {
                    var reciept = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                    if (reciept != null)
                    {
                        DatabaseHelper.Log($"RouteConfirmedSubmitted,{reciept.ContractAddress},{DateTime.UtcNow}");
                        return reciept.ContractAddress;
                    }
                    // Transacion not submitted. wait 3 seconds and check again
                    System.Threading.Thread.Sleep(_sleepBetweenRetry);
                    retry++;
                    if (retry > _maxRetry)
                        keepChecking = false;
                }
                return string.Empty;
            }).GetAwaiter().GetResult();

            return string.Empty;
        }

        public BigInteger GetBalance(string nodePublicKey, string nodePassword, string contractAddress)
        {
            var result = new BigInteger(0);
            Task.Run(async () =>
            {
                await UnlockAccount(nodePublicKey, nodePassword);

                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var getBalanceFunction = contract.GetFunction("getBalance");

                result = await getBalanceFunction.CallAsync<BigInteger>();
                return result;

            }).GetAwaiter().GetResult();

            return result;
        }

        public string GetParentContract(string nodePublicKey, string nodePassword, string contractAddress)
        {
            string result = string.Empty;
            Task.Run(async () =>
            {
                await UnlockAccount(nodePublicKey, nodePassword);
                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var getBalanceFunction = contract.GetFunction("getParentContract");

                result = await getBalanceFunction.CallAsync<string>();

                return result;

            }).GetAwaiter().GetResult();

            return result;
        }

    }
}
