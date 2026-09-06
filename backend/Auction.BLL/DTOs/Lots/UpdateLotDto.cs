namespace Auction.BLL.DTOs.Lots;

public class UpdateLotDto : CreateLotDto
{
    public decimal CurrentPrice { get; set; }
}
