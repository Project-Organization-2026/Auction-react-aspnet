using Auction.DAL.Enums;

namespace Auction.DAL.Entities;

public class Lot
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal MinBidStep { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public LotStatus Status { get; set; } = LotStatus.Draft;
    public AuctionSettlementMode SettlementMode { get; set; } =
        AuctionSettlementMode.OffChain;
    public long? ChainId { get; set; }
    public string? ContractAddress { get; set; }
    public string? OnChainAuctionId { get; set; }
    public string? CreationTransactionHash { get; set; }
    public string? SettlementTransactionHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign keys
    public int SellerId { get; set; }
    public int? WinnerId { get; set; }
    public int? CategoryId { get; set; }

    // Navigation properties
    public User Seller { get; set; } = null!;
    public User? Winner { get; set; }
    public Category? Category { get; set; }
    public ICollection<LotImage> Images { get; set; } = new List<LotImage>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
}
