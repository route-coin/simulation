pragma solidity ^0.4.8;

contract RouteCoin {

	// parent contract that this contract was generated based on.
	address parentContract;

	// this shows how many hups has happened until this contract was created.
	// for now we will limit this to be 5 hups. after 5 hups any nodes getting a whisper about a contract, will ignore.
	uint hupCount;
		
    // The public key of the buyer. Reza: we need to hash this.
    address buyer;   

    address seller;

    // Q: is this a Wallet address? an IP address? 
    // The destination of RREQ
    address finalDestination;  

    // The deadline when the contract will end automatically
    uint contractStartTime;

    // The duration of the contract will end automatically
    uint contractGracePeriod;

    enum State { Created, Expired, Completed, Aborted, RouteFound }
    State state;

    function RouteCoin(address _finalDestination, uint _contractGracePeriod, address _parentContract, uint _hupCount) 
        payable
    {
        buyer = msg.sender;
        contractStartTime = now;        
        finalDestination = _finalDestination;
        contractGracePeriod = _contractGracePeriod;
		parentContract = _parentContract;
        if(_parentContract != address(0x0))
        {
            RouteCoin m = RouteCoin(_parentContract);
            hupCount = m.getHupCount() + 1; 
        }
		else
		{
			hupCount = 0; 
		}
    }

    modifier require(bool _condition) {
        if (!_condition) throw;
        _;
    }

    modifier onlyBuyer() {
        if (msg.sender != buyer) throw;
        _;
    }

    modifier onlySeller() {
        if (msg.sender != seller) throw;
        _;
    }

    modifier expired() {
        if (now < contractStartTime + contractGracePeriod) throw;
        _;
    }

    modifier inState(State _state) {
        if (state != _state) throw;
        _;
    }

    function destinationAddressRouteFound()
        //expired // contract must be in the Created state to be able to foundDestinationAddress
        inState(State.Created)
        returns (State)
    {
        seller = msg.sender;
        routeFound();
        state = State.RouteFound;
        return state;
    }

    function destinationAddressRouteConfirmed()
        //onlyBuyer  // only buyer can confirm the working route 
        inState(State.RouteFound)  // contract must be in the Created state to be able to confirmPurchase
        payable
        returns (State)
    {
        routeAccepted();
        state = State.Completed;
        if (!seller.send(this.balance))
            throw;
        return state;
    }

    function abort()
        //onlyBuyer // only buyer can abort the contract
        inState(State.Created)  // contract must be in the Created state to be able to abort
        returns (State)
    {
        aborted();
        state = State.Aborted;
        return state;
    }

    function getState()
        returns (State)
    {
        return state;
    }

	 function getBalance()
	 	returns (uint)
     {
         return this.balance;
     }

	 function getParentContract()
	 	constant returns (address)
     {
         return parentContract;
     }

     function getHupCount()
	 	constant returns (uint)
     {
         return hupCount;
     }


    // Events
    event aborted();
    event routeFound();
    event routeAccepted();

}