using Auction.BLL.DTOs.Users;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Options;
using AutoMapper;

namespace Auction.BLL.Services;

public class UsersService
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;

    public UsersService(IRepositoryWrapper repositoryWrapper, IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId)
    {
        var user = await _repositoryWrapper.UsersRepository.GetProfileByIdAsync(userId);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        return _mapper.Map<UserProfileDto>(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        int userId,
        UpdateUserProfileDto dto)
    {
        ValidateProfile(dto);

        var user = await _repositoryWrapper.UsersRepository.GetFirstOrDefaultAsync(
            new QueryOptions<User>
            {
                Filter = item => item.Id == userId,
                AsNoTracking = false
            });

        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        var userWithSameName = await _repositoryWrapper.UsersRepository
            .GetByUserNameAsync(dto.UserName.Trim());
        if (userWithSameName is not null && userWithSameName.Id != userId)
        {
            throw new ArgumentException("This user name is already in use.");
        }

        var userWithSameEmail = await _repositoryWrapper.UsersRepository
            .GetByEmailAsync(dto.Email.Trim());
        if (userWithSameEmail is not null && userWithSameEmail.Id != userId)
        {
            throw new ArgumentException("This email is already in use.");
        }

        _mapper.Map(dto, user);
        await _repositoryWrapper.SaveChangesAsync();

        return await GetProfileAsync(userId);
    }

    public async Task<decimal> TopUpBalanceAsync(int userId, decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Top-up amount must be greater than zero.");
        }

        await using var transaction = await _repositoryWrapper.BeginTransactionAsync();
        var user = await _repositoryWrapper.UsersRepository.GetForUpdateAsync(userId);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        user.Balance += amount;
        await _repositoryWrapper.SaveChangesAsync();
        await transaction.CommitAsync();

        return user.Balance;
    }

    private static void ValidateProfile(UpdateUserProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserName))
        {
            throw new ArgumentException("User name is required.");
        }

        if (dto.UserName.Trim().Length > 256)
        {
            throw new ArgumentException("User name cannot exceed 256 characters.");
        }

        if (string.IsNullOrWhiteSpace(dto.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (dto.Email.Trim().Length > 256)
        {
            throw new ArgumentException("Email cannot exceed 256 characters.");
        }

        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
            .IsValid(dto.Email))
        {
            throw new ArgumentException("Email has an invalid format.");
        }
    }
}
