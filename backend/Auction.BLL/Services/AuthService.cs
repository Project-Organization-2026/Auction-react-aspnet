using Auction.BLL.DTOs.Auth;
using Auction.BLL.DTOs.Users;
using Auction.DAL.Entities;
using Auction.DAL.Enums;
using Auction.DAL.Repositories.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.ComponentModel.DataAnnotations;

namespace Auction.BLL.Services;

public class AuthService
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly JwtService _jwtService;
    private readonly IMapper _mapper;

    public AuthService(
        IRepositoryWrapper repositoryWrapper,
        JwtService jwtService,
        IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _jwtService = jwtService;
        _mapper = mapper;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterUserDto dto)
    {
        ValidateRegistration(dto);

        var userName = dto.UserName.Trim();
        var email = NormalizeEmail(dto.Email);

        if (await _repositoryWrapper.UsersRepository.ExistsByUserNameAsync(userName))
        {
            throw new InvalidOperationException("This user name is already in use.");
        }

        if (await _repositoryWrapper.UsersRepository.ExistsByEmailAsync(email))
        {
            throw new InvalidOperationException("This email is already in use.");
        }

        var user = new User
        {
            UserName = userName,
            Email = email,
            PasswordHash = PasswordService.HashPassword(dto.Password),
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow,
            RefreshToken = _jwtService.GenerateRefreshToken(),
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        await _repositoryWrapper.UsersRepository.CreateAsync(user);

        try
        {
            await _repositoryWrapper.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new InvalidOperationException(
                "A user with this email or user name already exists.",
                ex);
        }

        return CreateResponse(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrEmpty(dto.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var user = await _repositoryWrapper.UsersRepository
            .GetByEmailAsync(NormalizeEmail(dto.Email));

        if (user is null ||
            !PasswordService.VerifyPassword(dto.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        user.RefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        _repositoryWrapper.UsersRepository.Update(user);
        await _repositoryWrapper.SaveChangesAsync();

        return CreateResponse(user);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AccessToken) || string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            throw new ValidationException("Access token and refresh token are required.");
        }

        var principal = _jwtService.GetPrincipalFromExpiredToken(dto.AccessToken);
        if (principal is null)
        {
            throw new UnauthorizedAccessException("Invalid access token.");
        }

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user claim in token.");
        }

        await using var transaction = await _repositoryWrapper.BeginTransactionAsync();
        var user = await _repositoryWrapper.UsersRepository.GetForUpdateAsync(userId);
        if (user is null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        if (user.RefreshToken != dto.RefreshToken ||
            user.RefreshTokenExpiryTime == null ||
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        user.RefreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _repositoryWrapper.SaveChangesAsync();
        await transaction.CommitAsync();

        return CreateResponse(user);
    }

    public async Task RevokeRefreshTokenAsync(int userId)
    {
        await using var transaction = await _repositoryWrapper.BeginTransactionAsync();
        var user = await _repositoryWrapper.UsersRepository.GetForUpdateAsync(userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            await _repositoryWrapper.SaveChangesAsync();
            await transaction.CommitAsync();
        }
    }

    private AuthResponseDto CreateResponse(User user)
    {
        return new AuthResponseDto
        {
            AccessToken = _jwtService.GetAccessToken(user),
            RefreshToken = user.RefreshToken,
            User = _mapper.Map<UserSummaryDto>(user)
        };
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    private static void ValidateRegistration(RegisterUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName))
        {
            throw new ValidationException("User name is required.");
        }

        if (dto.UserName.Trim().Length > 256)
        {
            throw new ValidationException("User name cannot exceed 256 characters.");
        }

        if (string.IsNullOrWhiteSpace(dto.Email) ||
            !new EmailAddressAttribute().IsValid(dto.Email))
        {
            throw new ValidationException("Email has an invalid format.");
        }

        if (string.IsNullOrWhiteSpace(dto.Password) ||
            dto.Password.Length is < 8 or > 128)
        {
            throw new ValidationException("Password must contain from 8 to 128 characters.");
        }
    }
}
