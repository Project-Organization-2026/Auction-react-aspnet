using Auction.BLL.Constants;
using System.ComponentModel.DataAnnotations;

namespace Auction.BLL.DTOs.Users;

public class TopUpBalanceDto
{
    [Range(MonetaryLimits.MinAmountDouble, MonetaryLimits.MaxAmountDouble, ErrorMessage = "Amount must be between 0.01 and 9999999999999999.99.")]
    public decimal Amount { get; set; }
}
