using Auction.BLL.DTOs.Bids;
using Auction.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auction.API.Controllers;

[ApiController]
[Route("api")]
public class BidsController : ControllerBase
{
    private readonly BidsService _bidsService;

    public BidsController(BidsService bidsService)
    {
        _bidsService = bidsService;
    }

    [HttpGet("lots/{lotId:int}/bids", Name = "GetBidsByLotId")]
    public async Task<IActionResult> GetBidsByLotId(
        [FromRoute] int lotId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var bids = await _bidsService.GetBidsByLotIdAsync(lotId, page, pageSize);
        return Ok(bids);
    }

    [HttpPost("bids")]
    [Authorize]
    public async Task<IActionResult> CreateBid([FromBody] CreateBidDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            var bid = await _bidsService.CreateBidAsync(dto, userId);
            return CreatedAtRoute("GetBidsByLotId", new { lotId = dto.LotId }, bid);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
