using Auction.BLL.DTOs.Users;

namespace Auction.BLL.DTOs.Auth;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;
    public UserSummaryDto User { get; set; } = null!;
}
