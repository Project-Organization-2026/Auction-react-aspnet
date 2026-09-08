using Auction.DAL.Enums;

namespace Auction.DAL.Entities;

public class User
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.User;
    public decimal Balance { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Lot> CreatedLots { get; set; } = new List<Lot>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<Lot> WonLots { get; set; } = new List<Lot>();
}
