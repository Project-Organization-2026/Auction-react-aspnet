using Auction.DAL.Enums;

namespace Auction.BLL.DTOs.Bids;

public class BidDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public decimal? AmountEth { get; set; }
    public BidCurrency Currency { get; set; } = BidCurrency.Usd;
    public string? TxHash { get; set; }
    public string? WalletAddress { get; set; }
    public DateTime PlacedAt { get; set; }
    public int LotId { get; set; }
    public int? UserId { get; set; }
    public string? UserName { get; set; }
}
