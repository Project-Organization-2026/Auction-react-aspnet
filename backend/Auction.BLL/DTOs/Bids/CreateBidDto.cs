namespace Auction.BLL.DTOs.Bids;

public class CreateBidDto
{
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue)]
    public int LotId { get; set; }

    [System.ComponentModel.DataAnnotations.Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99")]
    public decimal Amount { get; set; }
}
