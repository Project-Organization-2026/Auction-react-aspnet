using Auction.BLL.DTOs.LotImages;
using Auction.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Auction.API.Controllers;

[ApiController]
[Route("api/lots")]
[Authorize]
public class LotImagesController : AuctionControllerBase
{
    private readonly LotImagesService _lotImagesService;
    private readonly IWebHostEnvironment? _environment;

    public LotImagesController(LotImagesService lotImagesService, IWebHostEnvironment? environment = null)
    {
        _lotImagesService = lotImagesService;
        _environment = environment;
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
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
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

    [HttpPost("{lotId:int}/images/upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(
        [FromRoute] int lotId,
        [FromForm] IFormFile file,
        [FromForm] bool isMain = false)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("No image file provided.");
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest("File size exceeds 10 MB limit.");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("Invalid image format. Allowed formats: .jpg, .jpeg, .png, .webp, .gif");
        }

        var webRoot = _environment?.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, "uploads", "lots");
        Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid():N}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/lots/{uniqueFileName}";
        try
        {
            var image = await _lotImagesService.AddImageToLotAsync(
                lotId,
                new AddLotImageDto { Url = relativeUrl, IsMain = isMain },
                userId);

            return StatusCode(StatusCodes.Status201Created, image);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
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
}
