import { expect } from "chai";
import { network } from "hardhat";

describe("Auction Smart Contract", function () {
  let ethers: any;
  let auction: any;
  let seller: any;
  let bidder1: any;
  let bidder2: any;

  const lotId = 42;
  const startingPrice = 1000n; // 1000 wei for simplicity
  const duration = 3600; // 1 hour

  beforeEach(async function () {
    const env = await network.create();
    ethers = env.ethers;
    const signers = await ethers.getSigners();
    seller = signers[0];
    bidder1 = signers[1];
    bidder2 = signers[2];

    const auctionFactory = await ethers.getContractFactory("Auction", seller);
    auction = await auctionFactory.deploy(lotId, startingPrice, duration);
  });

  describe("Initialization", function () {
    it("Should correctly set lotId, seller, and startingPrice", async function () {
      expect(await auction.lotId()).to.equal(lotId);
      expect(await auction.seller()).to.equal(seller.address);
      expect(await auction.startingPrice()).to.equal(startingPrice);
      expect(await auction.ended()).to.be.false;
      expect(await auction.highestBid()).to.equal(0n);
    });
  });

  describe("Bidding (placeBid)", function () {
    it("Should revert if bid is less than or equal to startingPrice", async function () {
      await expect(
        auction.connect(bidder1).placeBid({ value: 500n })
      ).to.be.revertedWithCustomError(auction, "BidNotHighEnough");

      await expect(
        auction.connect(bidder1).placeBid({ value: startingPrice })
      ).to.be.revertedWithCustomError(auction, "BidNotHighEnough");
    });

    it("Should accept valid first bid and emit HighestBidIncreased", async function () {
      const bidAmount = 1500n;
      await expect(auction.connect(bidder1).placeBid({ value: bidAmount }))
        .to.emit(auction, "HighestBidIncreased")
        .withArgs(bidder1.address, bidAmount);

      expect(await auction.highestBidder()).to.equal(bidder1.address);
      expect(await auction.highestBid()).to.equal(bidAmount);
    });

    it("Should record pending returns when outbid", async function () {
      await auction.connect(bidder1).placeBid({ value: 1500n });
      await auction.connect(bidder2).placeBid({ value: 2000n });

      expect(await auction.highestBidder()).to.equal(bidder2.address);
      expect(await auction.highestBid()).to.equal(2000n);
      expect(await auction.pendingReturns(bidder1.address)).to.equal(1500n);
    });
  });

  describe("Withdrawals (withdraw)", function () {
    it("Should revert if user has no pending returns", async function () {
      await expect(
        auction.connect(bidder1).withdraw()
      ).to.be.revertedWithCustomError(auction, "NoPendingReturns");
    });

    it("Should allow outbid users to withdraw their funds", async function () {
      await auction.connect(bidder1).placeBid({ value: 1500n });
      await auction.connect(bidder2).placeBid({ value: 2000n });

      expect(await auction.pendingReturns(bidder1.address)).to.equal(1500n);

      await auction.connect(bidder1).withdraw();
      expect(await auction.pendingReturns(bidder1.address)).to.equal(0n);
    });
  });

  describe("Ending Auction (endAuction)", function () {
    it("Should revert if auction duration has not passed", async function () {
      await expect(auction.endAuction()).to.be.revertedWithCustomError(
        auction,
        "AuctionNotYetEnded"
      );
    });
  });
});
