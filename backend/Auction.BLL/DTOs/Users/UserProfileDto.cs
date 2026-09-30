using Auction.BLL.DTOs.Lots;
using Auction.DAL.Enums;

namespace Auction.BLL.DTOs.Users;

public class UserProfileDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public UserRole Role { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<LotDto> CreatedLots { get; set; } = new List<LotDto>();
}
