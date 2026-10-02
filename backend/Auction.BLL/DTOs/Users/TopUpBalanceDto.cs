using Auction.BLL.Constants;
using System.ComponentModel.DataAnnotations;

namespace Auction.BLL.DTOs.Users;

public class TopUpBalanceDto
{
    [Range(typeof(decimal), MonetaryLimits.MinAmountString, MonetaryLimits.MaxAmountString)]
    public decimal Amount { get; set; }
}
