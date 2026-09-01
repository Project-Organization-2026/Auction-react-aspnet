namespace Auction.DAL.Entities;

public class LotImage
{
    public int Id { get; set; }
    public string Url { get; set; } = null!;
    public bool IsMain { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key
    public int LotId { get; set; }

    // Navigation property
    public Lot Lot { get; set; } = null!;
}
