pragma solidity ^0.4.0;

contract RouteCoin {
    // The public key of the buyer. Reza: we need to hash this.
    address private buyer;   

    address private seller;

    // Q: is this a Wallet address? an IP address? 
    // The destination of RREQ
    address private finalDestination;  

    // The deadline when the contract will end automatically
    uint private contractStartTime;

    // The duration of the contract will end automatically
    uint private contractGracePeriod;

    enum State { Created, Expired, Completed, Aborted, RouteFound }
    State public state;

    function RouteCoin(address _finalDestination, uint _contractGracePeriod) {
        buyer = msg.sender;
        contractStartTime = now;        
        finalDestination = _finalDestination;
        contractGracePeriod = _contractGracePeriod;
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
        //payable
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

    // Events
    event aborted();
    event routeFound();
    event routeAccepted();

}