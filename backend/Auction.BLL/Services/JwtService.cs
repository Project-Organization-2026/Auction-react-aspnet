using Auction.DAL.Entities;
using Auction.BLL.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Auction.BLL.Services;

public class JwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtSettings> options, ILogger<JwtService> logger)
    {
        _jwtSettings = options.Value;
        _logger = logger;
    }

    public string GetAccessToken(User user)
    {
        if (string.IsNullOrWhiteSpace(_jwtSettings.SecretKey))
        {
            _logger.LogError("JWT secret key is not configured.");
            throw new InvalidOperationException("JWT secret key is not configured.");
        }

        if (_jwtSettings.ExpireHours <= 0)
        {
            throw new InvalidOperationException("JWT expiration must be greater than zero.");
        }

        var claims = new List<Claim>
        {
            new("id", user.Id.ToString()),
            new("userName", user.UserName),
            new("email", user.Email),
            new("role", user.Role.ToString())
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpireHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
