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
            CreatedAt = DateTime.UtcNow
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

        return CreateResponse(user);
    }

    private AuthResponseDto CreateResponse(User user)
    {
        return new AuthResponseDto
        {
            AccessToken = _jwtService.GetAccessToken(user),
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
