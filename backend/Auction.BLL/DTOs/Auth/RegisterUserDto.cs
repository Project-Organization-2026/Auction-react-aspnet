using System.ComponentModel.DataAnnotations;

namespace Auction.BLL.DTOs.Auth;

public class RegisterUserDto
{
    [Required]
    [MaxLength(256)]
    public string UserName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = null!;
}
