
namespace Auction.BLL.DTOs.LotImages;

public class LotImageDto
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public bool IsMain { get; set; }
    public DateTime CreatedAt { get; set; }
    public int LotId { get; set; }
}
