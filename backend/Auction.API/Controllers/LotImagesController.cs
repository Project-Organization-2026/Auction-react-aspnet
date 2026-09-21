using Auction.BLL.DTOs.LotImages;
using Auction.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auction.API.Controllers;

[ApiController]
[Route("api/lots")]
[Authorize]
public class LotImagesController : ControllerBase
{
    private readonly LotImagesService _lotImagesService;

    public LotImagesController(LotImagesService lotImagesService)
    {
        _lotImagesService = lotImagesService;
    }

    [HttpPost("{lotId:int}/images")]
    public async Task<IActionResult> AddImage(
        [FromRoute] int lotId,
        [FromBody] AddLotImageDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            var image = await _lotImagesService.AddImageToLotAsync(lotId, dto, userId);
            return StatusCode(StatusCodes.Status201Created, image);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("images/{imageId:int}")]
    public async Task<IActionResult> DeleteImage([FromRoute] int imageId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            await _lotImagesService.DeleteImageAsync(imageId, userId);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPatch("{lotId:int}/images/{imageId:int}/set-main")]
    public async Task<IActionResult> SetMainImage(
        [FromRoute] int lotId,
        [FromRoute] int imageId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            var image = await _lotImagesService.SetMainImageAsync(imageId, lotId, userId);
            return Ok(image);
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
