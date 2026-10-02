using Auction.BLL.DTOs.Bids;
using Auction.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction.API.Controllers;

[ApiController]
[Route("api")]
public class BidsController : AuctionControllerBase
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
        try
        {
            var bids = await _bidsService.GetBidsByLotIdAsync(lotId, page, pageSize);
            return Ok(bids);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("bids")]
    [Authorize]
    public async Task<IActionResult> CreateBid([FromBody] CreateBidDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            var bid = await _bidsService.CreateBidAsync(dto, userId);
            return CreatedAtRoute("GetBidsByLotId", new { lotId = dto.LotId }, bid);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
