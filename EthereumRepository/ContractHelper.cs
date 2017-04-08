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
        private static string _abi = @"[{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteFound"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getBalance"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""getState"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""abort"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getHupCount"",""outputs"":[{""name"":"""",""type"":""uint256""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getBuyer"",""outputs"":[{""name"":"""",""type"":""address""}],""payable"":false,""type"":""function""},{""constant"":false,""inputs"":[],""name"":""destinationAddressRouteConfirmed"",""outputs"":[{""name"":"""",""type"":""uint8""}],""payable"":true,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getSeller"",""outputs"":[{""name"":"""",""type"":""address""}],""payable"":false,""type"":""function""},{""constant"":true,""inputs"":[],""name"":""getParentContract"",""outputs"":[{""name"":"""",""type"":""address""}],""payable"":false,""type"":""function""},{""inputs"":[{""name"":""_finalDestination"",""type"":""address""},{""name"":""_contractGracePeriod"",""type"":""uint256""},{""name"":""_parentContract"",""type"":""address""}],""payable"":true,""type"":""constructor""},{""anonymous"":false,""inputs"":[],""name"":""aborted"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeFound"",""type"":""event""},{""anonymous"":false,""inputs"":[],""name"":""routeAccepted"",""type"":""event""}]";

        private static string _byteCode = "60606040526040516060806105ac8339810160409081528151602083015191909201515b60028054600160a060020a03338116600160a060020a0319928316179092554260055560048054868416908316179055600684905560008054928416929091168217815590158061007b5750600160a060020a038216155b8061008d5750600160a060020a038216155b1561009c57600060015561012a565b81905080600160a060020a031663436565b16000604051602001526040518163ffffffff167c0100000000000000000000000000000000000000000000000000000000028152600401809050602060405180830381600087803b15156100fe57fe5b60325a03f1151561010b57fe5b5050604051516001908101908190556005901115905061012a57610000565b5b5b505050505b61046c806101406000396000f3006060604052361561007d5763ffffffff60e060020a600035041663047854d9811461007f57806312065fe0146100b35780631865c57d146100d557806335a063b414610109578063436565b11461013d578063603daf9a1461015f5780636c88c36c1461018b578063dbd0e1b6146101b7578063f1177712146101e3575bfe5b341561008757fe5b61008f61020f565b6040518082600481111561009f57fe5b60ff16815260200191505060405180910390f35b34156100bb57fe5b6100c36102a3565b60408051918252519081900360200190f35b34156100dd57fe5b61008f6102b2565b6040518082600481111561009f57fe5b60ff16815260200191505060405180910390f35b341561011157fe5b61008f6102bc565b6040518082600481111561009f57fe5b60ff16815260200191505060405180910390f35b341561014557fe5b6100c361034a565b60408051918252519081900360200190f35b341561016757fe5b61016f610351565b60408051600160a060020a039092168252519081900360200190f35b61008f610361565b6040518082600481111561009f57fe5b60ff16815260200191505060405180910390f35b34156101bf57fe5b61016f610420565b60408051600160a060020a039092168252519081900360200190f35b34156101eb57fe5b61016f610430565b60408051600160a060020a039092168252519081900360200190f35b600080805b60075460ff16600481111561022557fe5b1461022f57610000565b6003805473ffffffffffffffffffffffffffffffffffffffff191633600160a060020a03161790556040517f78d20fa24b6a0a3596e34219ca2fd4ce740f5d3cce342d6b1d76bd879491bf7290600090a1600780546004919060ff19166001835b021790555060075460ff1691505b5b5090565b600160a060020a033016315b90565b60075460ff165b90565b60025460009033600160a060020a039081169116146102da57610000565b6000805b60075460ff1660048111156102ef57fe5b146102f957610000565b6040517f80b62b7017bb13cf105e22749ee2a06a417ffba8c7f57b665057e0f3c2e925d990600090a1600780546003919060ff1916600183610290565b021790555060075460ff1691505b5b505b90565b6001545b90565b600254600160a060020a03165b90565b60045460009033600160a060020a0390811691161461037f57610000565b6004805b60075460ff16600481111561039457fe5b1461039e57610000565b6040517f17e8425f2f0f52156cb58fae3262b87ffe900164617ac332659a4b0e2d8434f590600090a1600780546002919060ff19166001835b0217905550600354604051600160a060020a039182169130163180156108fc02916000818181858888f19350505050151561041157610000565b60075460ff1691505b5b505b90565b600354600160a060020a03165b90565b600054600160a060020a03165b905600a165627a7a72305820420c15277507bd1a129db4f97da0ea543a6290358bb450d5a79f65440d97f7de0029";
        private static string _getAddress = "./geth.ipc";
        private static int _maxRetry = 20;
        private static int _sleepBetweenRetry = 15000;
        private static HexBigInteger _accountUnlockTime = new HexBigInteger(3000);

        public async Task<bool> UnlockAccount(string publicKey, string password)
        {
            var ipcClient = new IpcClient(_getAddress);
            var web3 = new Web3(ipcClient);

            // Unlock the caller's account with the given password
            var unlockResult = await web3.Personal.UnlockAccount.SendRequestAsync(publicKey, password, _accountUnlockTime);

            //DatabaseHelper.Log($"Account unlocked: {publicKey}");

            return unlockResult;
        } 

        public string CreateContract(string nodePublicKey, string nodePassword, Int64 balance, string destinationAddress, int contractGracePeriod, string parentContract)
        {
            var contractAddress = string.Empty;
            // create a contract
            Task.Run(() =>
            {
                try
                {

                    contractAddress = DatabaseHelper.CreateContract(nodePublicKey, balance, destinationAddress, contractGracePeriod, parentContract);

                    //await UnlockAccount(nodePublicKey, nodePassword);

                    //var ipcClient = new IpcClient(_getAddress);
                    //var web3 = new Web3(ipcClient);
                    //var transactionHash = await web3.Eth.DeployContract.SendRequestAsync(_abi, _byteCode, nodePublicKey, new HexBigInteger(900000), balance, new object[] { destinationAddress, contractGracePeriod, parentContract });
                    //DatabaseHelper.Log(nodePublicKey, $"Contract transaction submitted. trx:{transactionHash}");
                    //var keepChecking = true;
                    //var retry = 0;
                    //while (keepChecking)
                    //{
                    //    var reciept = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                    //    if (reciept != null)
                    //    {

                    //        DatabaseHelper.Log(nodePublicKey, $"Contract submitted. Contract PublicKey:{reciept.ContractAddress}", $"{nodePublicKey},{reciept.ContractAddress},{parentContract},ContractCreated,{reciept.CumulativeGasUsed.Value.ToString()},{balance.Value.ToString()},{DateTime.UtcNow.ToString("hh:mm:ss")}");
                    //        contractAddress = reciept.ContractAddress;
                    //        return contractAddress;
                    //    }
                    //    // Transacion not submitted. wait 3 seconds and check again
                    //    System.Threading.Thread.Sleep(_sleepBetweenRetry);
                    //    retry++;
                    //    if (retry > _maxRetry)
                    //        keepChecking = false;
                    //}
                    return contractAddress;
                }
                catch (Exception ex)
                {
                    DatabaseHelper.Log(nodePublicKey, $"Error: {ex.Message}");
                    return contractAddress;
                }
            }).GetAwaiter().GetResult();

            return contractAddress;
        }

      
        public string test(string nodePublicKey, string nodePassword)
        {

            var transactionHash = string.Empty;
            Task.Run(async () =>
            {
                try
                {
                    var ipcClient = new IpcClient(_getAddress);
                    var web3 = new Web3(ipcClient);

                    await UnlockAccount(nodePublicKey, nodePassword);

                    var abi = "[{'inputs':[],'payable':true,'type':'constructor'}]";

                    var byteCode = "0x60606040526108f2806100126000396000f3606060405260e060020a6000350463472ad331811461004a5780637996c88714610058578063b4821203146102ff578063dbda4c0814610400578063fa82518514610449575b610002565b34610002576104af60005481565b346100025760408051602080820183526000808352835180830185528181528451808401865282815285518085018752838152865180860188528481528751808701895285815288518088018a5286815289518089018b528781528a51808a018c528881528b51998a018c52888a5288549b516104c19c989a979996989597959694959394929391929087908059106100ee5750595b908082528060200260200182016040528015610105575b509550866040518059106101165750595b90808252806020026020018201604052801561012d575b5094508660405180591061013e5750595b908082528060200260200182016040528015610155575b509350866040518059106101665750595b90808252806020026020018201604052801561017d575b5092508660405180591061018e5750595b9080825280602002602001820160405280156101a5575b509150600090505b60005481101561065757600180548290811015610002579060005260206000209060050201600050548651879083908110156100025760209081029091010152600180548290811015610002579060005260206000209060050201600050600101548551600160a060020a03909116908690839081101561000257600160a060020a03909216602092830290910190910152600180548290811015610002579060005260206000209060050201600050600201600050548482815181101561000257602090810290910101526001805482908110156100025790600052602060002090600502016000506003016000505483828151811015610002576020908102909101015260018054829081101561000257906000526020600020906005020160005060040154825160ff9091169083908390811015610002579115156020928302909101909101526001016101ad565b6105f760043560243560008082151561032f57600280546000908110156100025760009182526020909120015492505b61066a84846040805160a081018252600080825260208201819052918101829052606081018290526080810182905281905b6000548210156106c05784600160a060020a0316600160005083815481101561000257906000526020600020906005020160005060010154600160a060020a03161480156103d1575083600160005083815481101561000257906000526020600020906005020160005060020154145b156107a7576001805483908110156100025790600052602060002090600502016000505492505b505092915050565b346100025761060b600435600280546001810180835582818380158290116106a6576000838152602090206106a69181019083015b808211156106bc5760008155600101610435565b34610002576040805160208082018352600082526002805484518184028101840190955280855261060d94928301828280156104a557602002820191906000526020600020905b81548152600190910190602001808311610490575b5050505050905090565b60408051918252519081900360200190f35b60405180806020018060200180602001806020018060200186810386528b8181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f15090500186810385528a8181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018681038452898181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018681038352888181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050018681038252878181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050019a505050505050505050505060405180910390f35b604080519115158252519081900360200190f35b005b60405180806020018281038252838181518152602001915080519060200190602002808383829060006004602084601f0104600302600f01f1509050019250505060405180910390f35b50939a9299509097509550909350915050565b905034600160005060018303815481101561000257906000526020600020906005020160005060030180549091019055600191505b5092915050565b5050506000928352506020909120018190555b50565b5090565b6107b2858560a06040519081016040528060008152602001600081526020016000815260200160008152602001600081526020015060a0604051908101604052806000815260200160008152602001600081526020016000815260200160008152602001506107bd8360008181526003602052604090205460ff60a060020a9091041615156106b9576000818152600360205260409020805474ff0000000000000000000000000000000000000000191660a060020a179055600280546001810180835582818380158290116106a6576000838152602090206106a6918101908301610435565b600190910190610361565b8051935090506103f8565b60008054600190810191829055600160a060020a0386166020840152604083018590529082528054808201808355828183801582901161085e5760050281600502836000526020600020918201910161085e91905b808211156106bc57600080825560018201805473ffffffffffffffffffffffffffffffffffffffff1916905560028201819055600382015560048101805460ff19169055600501610812565b50505060009283525060209182902083516005909202019081559082015160018201805473ffffffffffffffffffffffffffffffffffffffff19166c0100000000000000000000000092830292909204919091179055604082015160028201556060820151600382015560808201516004909101805460ff191660f860020a9283029290920491909117905590508061069f56";

                    //var web3 = new Web3Geth(new ManagedAccount(addressFrom, pass));

                    transactionHash = await web3.Eth.DeployContract.SendRequestAsync(abi, byteCode, nodePublicKey, new HexBigInteger(1999990), new HexBigInteger(10), new object[] { });
                    return transactionHash;
                }
                catch (Exception ex)
                {
                    DatabaseHelper.Log(nodePublicKey, $"Error: {ex.Message}");
                    return transactionHash;
                }
                }).GetAwaiter().GetResult();

            return transactionHash;
        }

        public string GetBuyer(string nodePublicKey, string nodePassword, string contractAddress)
        {
            string result = string.Empty;
            Task.Run(async () =>
            {
                await UnlockAccount(nodePublicKey, nodePassword);
                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var getBalanceFunction = contract.GetFunction("getBuyer");

                result = await getBalanceFunction.CallAsync<string>();

                return result;

            }).GetAwaiter().GetResult();

            return result;
        }

        public string GetSeller(string nodePublicKey, string nodePassword, string contractAddress)
        {
            string result = string.Empty;
            Task.Run(async () =>
            {
                await UnlockAccount(nodePublicKey, nodePassword);
                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var getBalanceFunction = contract.GetFunction("getSeller");

                result = await getBalanceFunction.CallAsync<string>();

                return result;

            }).GetAwaiter().GetResult();

            return result;
        }

        public string RouteFound(string nodePublicKey, string nodePassword, string contractAddress, string callerAddress, string parentContract)
        {
            var result = string.Empty;
            Task.Run(async () =>
            {

                await UnlockAccount(nodePublicKey, nodePassword);

                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var destinationAddressRouteFoundFunction = contract.GetFunction("destinationAddressRouteFound");

                var transactionHash = await destinationAddressRouteFoundFunction.SendTransactionAsync(callerAddress);
                DatabaseHelper.Log(nodePublicKey, $"RouteFound transaction submitted. trx: {transactionHash}");
                var keepChecking = true;
                var retry = 0;
                while (keepChecking)
                {
                    var reciept = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                    if (reciept != null)
                    {
                        DatabaseHelper.Log(nodePublicKey, $"RouteFound submitted. Block number:{reciept.BlockNumber.Value.ToString()}", $"{nodePublicKey},{contractAddress},{parentContract},RouteFound,{reciept.CumulativeGasUsed.Value.ToString()},{0},{DateTime.UtcNow.ToString("hh:mm:ss")}");
                        result = reciept.BlockNumber.Value.ToString();
                        return result;
                    }
                    // Transacion not submitted. wait 3 seconds and check again
                    System.Threading.Thread.Sleep(_sleepBetweenRetry);
                    retry++;
                    if (retry > _maxRetry)
                        keepChecking = false;
                }
                return result;

            }).GetAwaiter().GetResult();

            return result;
        }

        public string RouteConfirmed(string nodePublicKey, string nodePassword, string contractAddress, string callerAddress, string parentContract)
        {
            var result = string.Empty;
            Task.Run(async () =>
            {
                await UnlockAccount(nodePublicKey, nodePassword);

                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var destinationAddressRouteFoundFunction = contract.GetFunction("destinationAddressRouteConfirmed");

                var transactionHash = await destinationAddressRouteFoundFunction.SendTransactionAsync(callerAddress);

                DatabaseHelper.Log(nodePublicKey, $"RouteConfirm transaction submitted. trx: {transactionHash}");
                var keepChecking = true;
                var retry = 0;
                while (keepChecking)
                {
                    var reciept = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
                    if (reciept != null)
                    {
                        DatabaseHelper.Log(nodePublicKey, $"RouteConfirm Submitted. Block number:{reciept.BlockNumber.Value.ToString()}", $"{nodePublicKey},{contractAddress},{parentContract},RouteConfirmed,{reciept.CumulativeGasUsed.Value.ToString()},{0},{DateTime.UtcNow.ToString("hh:mm:ss")}");
                        result = reciept.BlockNumber.Value.ToString();
                        return result;
                    }
                    // Transacion not submitted. wait 3 seconds and check again
                    System.Threading.Thread.Sleep(_sleepBetweenRetry);
                    retry++;
                    if (retry > _maxRetry)
                        keepChecking = false;
                }
                return result;
            }).GetAwaiter().GetResult();

            return result;
        }

        public Int64 GetBalance(string nodePublicKey, string nodePassword, string contractAddress)
        {
            Int64 result = 0;
            Task.Run(async () =>
            {
                await UnlockAccount(nodePublicKey, nodePassword);

                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var getBalanceFunction = contract.GetFunction("getBalance");

                result = await getBalanceFunction.CallAsync<Int64>();
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

        public BigInteger GetState(string publicKey, string password, string contractAddress)
        {
            BigInteger result = 0;
            Task.Run(async () =>
            {
                await UnlockAccount(publicKey, password);
                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var getBalanceFunction = contract.GetFunction("getState");

                result = await getBalanceFunction.CallAsync<BigInteger>();

                return result;

            }).GetAwaiter().GetResult();

            return result;
        }

        

        public BigInteger GetHupCount(string publicKey, string password, string contractAddress)
        {
            BigInteger result = 0;
            Task.Run(async () =>
            {
                await UnlockAccount(publicKey, password);
                var ipcClient = new IpcClient(_getAddress);
                var web3 = new Web3(ipcClient);

                var contract = web3.Eth.GetContract(_abi, contractAddress);
                var getBalanceFunction = contract.GetFunction("getHupCount");

                result = await getBalanceFunction.CallAsync<BigInteger>();

                return result;

            }).GetAwaiter().GetResult();

            return result;
        }


    }
}
