using System.ComponentModel.DataAnnotations;

namespace Auction.BLL.DTOs.Bids;

public class CreateOnChainBidDto
{
    [Required]
    public int LotId { get; set; }

    [Required]
    [RegularExpression("^0x[a-fA-F0-9]{64}$", ErrorMessage = "Invalid transaction hash format.")]
    public string TxHash { get; set; } = null!;

    [Required]
    [Range(0.0001, 1000000, ErrorMessage = "ETH bid amount must be greater than 0.")]
    public decimal AmountEth { get; set; }

    [Required]
    [RegularExpression("^0x[a-fA-F0-9]{40}$", ErrorMessage = "Invalid Ethereum wallet address.")]
    public string WalletAddress { get; set; } = null!;
}
