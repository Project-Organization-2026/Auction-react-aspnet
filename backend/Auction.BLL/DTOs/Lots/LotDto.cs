using Auction.BLL.DTOs.Categories;
using Auction.BLL.DTOs.Users;
using Auction.DAL.Entities;
using Auction.DAL.Enums;

namespace Auction.BLL.DTOs.Lots;

public class LotDto
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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public UserSummaryDto Seller { get; set; } = null!;
    public UserSummaryDto? Winner { get; set; }
    public CategoryDto? Category { get; set; }
}
