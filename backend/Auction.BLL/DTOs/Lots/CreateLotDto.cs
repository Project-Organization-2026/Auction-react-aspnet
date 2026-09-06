using Auction.DAL.Enums;

namespace Auction.BLL.DTOs.Lots;

public class CreateLotDto
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public decimal StartingPrice { get; set; }
    public decimal MinBidStep { get; set; }
    public DateTime EndTime { get; set; }
    public LotStatus Status { get; set; } = LotStatus.Draft;

    public int SellerId { get; set; }
    public int CategoryId { get; set; }
}
