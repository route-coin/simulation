using Nethereum.JsonRpc.IpcClient;
using Nethereum.Web3;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Nethereum.Hex.HexTypes;
using System.Numerics;
using System.Collections.Generic;
using DatabaseRepository;

namespace RouteCoin
{
    public class ContractHelper
    {
        // Smart Contract API (json interface) and byte code
        private static string _abi = @"[{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteFound"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getBalance"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getState"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""abort"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteConfirmed"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":true,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getParentContracts"",""outputs"":[{""name"":"""",""type"":""address[10]""}],""payable"":false,""type"":""function""},{""inputs"":[{""name"":""_finalDestination"",""type"":""address""},{""name"":""_contractGracePeriod"",""type"":""uint256""},{""name"":""_parentContracts"",""type"":""address[10]""}],""payable"":true,""type"":""constructor""},{""anonymous"":false,""inputs"":[],""name"":""aborted"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeFound"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeAccepted"",""type"":""event""}]";
        private static string _byteCode = "606060408190526000805460ff1916600a179055610180806104958339810160409081528151602083015190929091015b600b8054600160a060020a03338116600160a060020a03199283161790925542600e55600d805492861692909116919091179055600f82905560005b60005460ff90811690821610156100d4578160ff8216600a811061008c57fe5b6020020151600160ff8316600a81106100a157fe5b0160005b6101000a815481600160a060020a030219169083600160a060020a03160217905550808060010191505061006c565b5b505050505b6103ac806100e96000396000f3006060604052361561005c5763ffffffff60e060020a600035041663047854d9811461005e57806312065fe0146100925780631865c57d146100b457806335a063b4146100e85780636c88c36c1461011c578063d53a6dea14610148575bfe5b341561006657fe5b61006e610196565b6040518082600481111561007e57fe5b60ff16815260200191505060405180910390f35b341561009a57fe5b6100a2610209565b60408051918252519081900360200190f35b34156100bc57fe5b61006e610218565b6040518082600481111561007e57fe5b60ff16815260200191505060405180910390f35b34156100f057fe5b61006e610222565b6040518082600481111561007e57fe5b60ff16815260200191505060405180910390f35b61006e61028e565b6040518082600481111561007e57fe5b60ff16815260200191505060405180910390f35b341561015057fe5b61015861030e565b60405180826101408083835b80518252602083111561018457601f199092019160209182019101610164565b50505090500191505060405180910390f35b600c805473ffffffffffffffffffffffffffffffffffffffff191633600160a060020a03161790556040516000907f78d20fa24b6a0a3596e34219ca2fd4ce740f5d3cce342d6b1d76bd879491bf72908290a1601080546004919060ff19166001835b02179055505060105460ff165b90565b600160a060020a033016315b90565b60105460ff165b90565b600080805b60105460ff16600481111561023857fe5b1461024257610000565b6040517f80b62b7017bb13cf105e22749ee2a06a417ffba8c7f57b665057e0f3c2e925d990600090a1601080546003919060ff19166001835b021790555060105460ff1691505b5b5090565b6040516000907f17e8425f2f0f52156cb58fae3262b87ffe900164617ac332659a4b0e2d8434f5908290a1601080546002919060ff19166001835b0217905550600c54604051600160a060020a039182169130163180156108fc02916000818181858888f19350505050151561030357610000565b5060105460ff165b90565b610316610356565b6040805161014081019182905290600190600a9082845b8154600160a060020a0316815260019091019060200180831161032d575b505050505090505b90565b61014060405190810160405280600a905b60008152600019909101906020018161036757905050905600a165627a7a72305820b2a9167d0a856b6d14f0d7bf18b4a616e4745e6897ca2ff3d6dce4b4a7388d330029";
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
                    //balance = new HexBigInteger(200000);
                    var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _byteCode, nodePublicKey, new HexBigInteger(900000), balance, new object[] { destinationAddress, contractGracePeriod, parentContracts });
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
                            contractAddress = reciept.ContractAddress;
                            return contractAddress;
                        }
                        // Transacion not submitted. wait 3 seconds and check again
                        System.Threading.Thread.Sleep(3000);
                        retry++;
                        if (retry > maxRetry)
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
                DatabaseHelper.Log($"RouteFound,{transactionHash},{DateTime.UtcNow}");
                var keepChecking = true;
                var maxRetry = 20;
                var retry = 0;
                while (keepChecking)
                {
                    var reciept = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                    if (reciept != null)
                    {
                        DatabaseHelper.Log($"RouteFoundSubmitted,{reciept.ContractAddress},{DateTime.UtcNow}");
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

                DatabaseHelper.Log($"RouteConfirmed,{transactionHash},{DateTime.UtcNow}");
                var keepChecking = true;
                var maxRetry = 20;
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
                    System.Threading.Thread.Sleep(3000);
                    retry++;
                    if (retry > maxRetry)
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

        public List<string> GetParentContracts(string nodePublicKey, string nodePassword, string contractAddress)
        {
            var result = new List<string>();
            Task.Run(async () =>
            {
                await UnlockAccount(nodePublicKey, nodePassword);
                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var getBalanceFunction = contract.GetFunction("getParentContracts");

                result = await getBalanceFunction.CallAsync<List<string>>();

                return result;

            }).GetAwaiter().GetResult();

            return result;
        }

    }
}
