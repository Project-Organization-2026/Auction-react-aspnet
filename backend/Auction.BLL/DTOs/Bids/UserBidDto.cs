using Auction.DAL.Enums;

namespace Auction.BLL.DTOs.Bids;

public class UserBidDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public decimal? AmountEth { get; set; }
    public BidCurrency Currency { get; set; } = BidCurrency.Usd;
    public string? TxHash { get; set; }
    public DateTime PlacedAt { get; set; }
    public int LotId { get; set; }
    public string LotTitle { get; set; } = string.Empty;
    public decimal LotCurrentPrice { get; set; }
    public LotStatus LotStatus { get; set; }
    public DateTime LotEndTime { get; set; }
    public string? LotMainImageUrl { get; set; }
}
