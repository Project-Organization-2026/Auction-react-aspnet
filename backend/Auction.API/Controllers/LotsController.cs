using Auction.BLL.DTOs.Lots;
using Auction.BLL.Services;
using Auction.DAL.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auction.API.Controllers;

[ApiController]
[Route("api/lots")]
public class LotsController : ControllerBase
{
    private readonly LotsService _lotsService;

    public LotsController(LotsService lotsService)
    {
        _lotsService = lotsService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllLots(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] LotStatus? status = null)
    {
        return Ok(await _lotsService.GetAllAsync(
            page,
            pageSize,
            search,
            categoryId,
            status));
    }

    [HttpGet("{id:int}", Name = nameof(GetLotById))]
    [AllowAnonymous]
    public async Task<IActionResult> GetLotById([FromRoute] int id)
    {
        try
        {
            return Ok(await _lotsService.GetByIdAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateLot([FromBody] CreateLotDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            var lot = await _lotsService.CreateLotAsync(dto, userId);
            return CreatedAtRoute(nameof(GetLotById), new { id = lot.Id }, lot);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdateLot(
        [FromRoute] int id,
        [FromBody] UpdateLotDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            return Ok(await _lotsService.UpdateLotAsync(dto, id, userId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeleteLot([FromRoute] int id)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            await _lotsService.DeleteLotAsync(id, userId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("{id:int}/close")]
    [Authorize]
    public async Task<IActionResult> CloseLot([FromRoute] int id)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            return Ok(await _lotsService.CloseLotAsync(id, userId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
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
