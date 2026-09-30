using System.ComponentModel.DataAnnotations;

namespace Auction.BLL.DTOs.Users;

public class TopUpBalanceDto
{
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }
}
