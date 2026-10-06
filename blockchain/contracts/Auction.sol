// SPDX-License-Identifier: MIT
pragma solidity ^0.8.24;

import "@openzeppelin/contracts/utils/ReentrancyGuard.sol";

contract Auction is ReentrancyGuard {
    uint256 public immutable lotId;
    address payable public immutable seller;
    uint256 public immutable startingPrice;
    uint256 public immutable auctionEndTime;
    address public immutable operator;

    uint256 public minimumBid;

    address public highestBidder;
    uint256 public highestBid;

    mapping(address => uint256) public pendingReturns;

    bool public ended;

    event HighestBidIncreased(address indexed bidder, uint256 amount);
    event FiatBidRecorded(uint256 minimumBid);
    event AuctionEnded(address winner, uint256 amount);

    error AuctionAlreadyEnded();
    error BidNotHighEnough(uint256 currentHighestBid);
    error AuctionNotYetEnded();
    error AuctionEndAlreadyCalled();
    error TransferFailed();
    error NoPendingReturns();
    error OnlyOperator();

    constructor(
        uint256 _lotId,
        uint256 _startingPrice,
        uint256 _durationInSeconds
    ) {
        lotId = _lotId;
        seller = payable(msg.sender);
        operator = msg.sender;
        startingPrice = _startingPrice;
        minimumBid = _startingPrice;
        auctionEndTime = block.timestamp + _durationInSeconds;
    }

    function placeBid() external payable {
        if (block.timestamp >= auctionEndTime) {
            revert AuctionAlreadyEnded();
        }

        if (msg.value <= minimumBid) {
            revert BidNotHighEnough(minimumBid);
        }

        if (highestBidder != address(0)) {
            pendingReturns[highestBidder] += highestBid;
        }

        highestBidder = msg.sender;
        highestBid = msg.value;
        minimumBid = msg.value;

        emit HighestBidIncreased(msg.sender, msg.value);
    }

    // The application records a USD bid only after raising the on-chain ETH floor.
    // Any ETH bid it outbids becomes available through withdraw().
    function recordFiatBid(uint256 _minimumBid) external {
        if (msg.sender != operator) revert OnlyOperator();
        if (block.timestamp >= auctionEndTime) revert AuctionAlreadyEnded();
        if (_minimumBid <= minimumBid) revert BidNotHighEnough(minimumBid);

        if (highestBidder != address(0)) {
            pendingReturns[highestBidder] += highestBid;
            highestBidder = address(0);
            highestBid = 0;
        }

        minimumBid = _minimumBid;
        emit FiatBidRecorded(_minimumBid);
    }

    function withdraw() external nonReentrant returns (bool) {
        uint256 amount = pendingReturns[msg.sender];
        if (amount == 0) {
            revert NoPendingReturns();
        }

        pendingReturns[msg.sender] = 0;

        (bool success, ) = payable(msg.sender).call{value: amount}("");
        if (!success) {
            pendingReturns[msg.sender] = amount;
            revert TransferFailed();
        }

        return true;
    }

    function endAuction() external nonReentrant {
        if (block.timestamp < auctionEndTime) {
            revert AuctionNotYetEnded();
        }
        if (ended) {
            revert AuctionEndAlreadyCalled();
        }

        ended = true;
        emit AuctionEnded(highestBidder, highestBid);

        if (highestBid > 0) {
            (bool success, ) = seller.call{value: highestBid}("");
            if (!success) {
                revert TransferFailed();
            }
        }
    }
}
