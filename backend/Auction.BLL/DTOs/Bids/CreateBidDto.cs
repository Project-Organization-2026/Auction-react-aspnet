// TODO: Add the request DTO for creating a bid.
// Suggested fields: LotId and Amount. Do not accept UserId from the request body;
// the authenticated user's id must come from the claims in BidsController.

namespace Auction.BLL.DTOs.Bids;

public class CreateBidDto
{
    public int LotId { get; set; }
    public decimal Amount { get; set; }
}