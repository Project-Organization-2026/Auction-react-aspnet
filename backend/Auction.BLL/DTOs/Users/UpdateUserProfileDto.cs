using System.ComponentModel.DataAnnotations;

namespace Auction.BLL.DTOs.Users;

public class UpdateUserProfileDto
{
    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = null!;
}
