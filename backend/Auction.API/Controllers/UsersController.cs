using Auction.BLL.DTOs.Users;
using Auction.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Auction.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UsersService _usersService;

    public UsersController(UsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUserProfile()
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            return Ok(await _usersService.GetProfileAsync(userId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost("me/balance")]
    public async Task<IActionResult> TopUpBalance([FromBody] TopUpBalanceDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            var balance = await _usersService.TopUpBalanceAsync(userId, dto.Amount);
            return Ok(new { balance });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUserProfile(
        [FromBody] UpdateUserProfileDto dto)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized("User ID claim is missing or invalid.");
        }

        try
        {
            return Ok(await _usersService.UpdateProfileAsync(userId, dto));
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

    private bool TryGetUserId(out int userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out userId);
    }
}
