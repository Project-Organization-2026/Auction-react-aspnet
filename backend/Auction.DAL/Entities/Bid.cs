namespace Auction.DAL.Entities;

public class Bid
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

    // Foreign keys
    public int LotId { get; set; }
    public int UserId { get; set; }

    // Navigation properties
    public Lot Lot { get; set; } = null!;
    public User User { get; set; } = null!;
}
