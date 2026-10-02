using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auction.API.Controllers;

public abstract class AuctionControllerBase : ControllerBase
{
    protected bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
