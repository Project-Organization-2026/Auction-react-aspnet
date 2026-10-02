using Auction.BLL.Constants;
using Auction.DAL.Enums;

namespace Auction.BLL.DTOs.Lots;

public class CreateLotDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MaxLength(512)]
    public string Title { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.MaxLength(5000)]
    public string Description { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(
        typeof(decimal),
        MonetaryLimits.MinAmountString,
        MonetaryLimits.MaxAmountString)]
    public decimal StartingPrice { get; set; }

    [System.ComponentModel.DataAnnotations.Range(
        typeof(decimal),
        MonetaryLimits.MinAmountString,
        MonetaryLimits.MaxAmountString)]
    public decimal MinBidStep { get; set; }

    public DateTime EndTime { get; set; }
    public LotStatus Status { get; set; } = LotStatus.Draft;

    public int? CategoryId { get; set; }
}
