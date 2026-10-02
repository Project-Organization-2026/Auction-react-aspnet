using System.ComponentModel.DataAnnotations;

namespace Auction.BLL.DTOs.Auth;

public class RegisterUserDto
{
    [Required(ErrorMessage = "Ім'я користувача є обов'язковим.")]
    [MaxLength(256, ErrorMessage = "Ім'я користувача не може перевищувати 256 символів.")]
    public string UserName { get; set; } = null!;

    [Required(ErrorMessage = "Електронна пошта є обов'язковою.")]
    [EmailAddress(ErrorMessage = "Введіть коректну адресу електронної пошти.")]
    [MaxLength(256, ErrorMessage = "Email не може перевищувати 256 символів.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Пароль є обов'язковим.")]
    [MinLength(8, ErrorMessage = "Пароль має містити щонайменше 8 символів.")]
    [MaxLength(128, ErrorMessage = "Пароль не може перевищувати 128 символів.")]
    public string Password { get; set; } = null!;
}
