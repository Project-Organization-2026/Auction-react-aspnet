using Auction.BLL.Constants;
using Auction.BLL.DTOs.Common;
using Auction.BLL.DTOs.Lots;
using Auction.DAL.Entities;
using Auction.DAL.Enums;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Options;
using AutoMapper;

namespace Auction.BLL.Services;

public class LotsService
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;

    public LotsService(IRepositoryWrapper repositoryWrapper, IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<LotDto>> GetAllAsync(
        int page = 1,
        int pageSize = 20,
        string? search = null,
        int? categoryId = null,
        LotStatus? status = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _repositoryWrapper.LotsRepository.GetPagedAsync(
            page,
            pageSize,
            search,
            categoryId,
            status);

        return new PagedResultDto<LotDto>
        {
            Items = _mapper.Map<IReadOnlyList<LotDto>>(result.Items),
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    public async Task<LotDto> GetByIdAsync(int id)
    {
        var lot = await _repositoryWrapper.LotsRepository.GetDetailsByIdAsync(id);
        if (lot is null)
        {
            throw new KeyNotFoundException($"Lot with ID {id} not found.");
        }

        return _mapper.Map<LotDto>(lot);
    }

    public async Task<LotDto> CreateLotAsync(CreateLotDto dto, int sellerId)
    {
        ValidateLot(dto);

        var seller = await _repositoryWrapper.UsersRepository.GetFirstOrDefaultAsync(
            new QueryOptions<User>
            {
                Filter = user => user.Id == sellerId,
                AsNoTracking = true
            });
        if (seller is null)
        {
            throw new KeyNotFoundException($"User with ID {sellerId} not found.");
        }

        await EnsureCategoryExistsAsync(dto.CategoryId);

        var lot = _mapper.Map<Lot>(dto);
        lot.SellerId = sellerId;
        lot.CurrentPrice = dto.StartingPrice;
        lot.StartTime = DateTime.UtcNow;
        lot.EndTime = NormalizeUtc(dto.EndTime);
        lot.CreatedAt = DateTime.UtcNow;

        await _repositoryWrapper.LotsRepository.CreateAsync(lot);
        await _repositoryWrapper.SaveChangesAsync();

        return await GetByIdAsync(lot.Id);
    }

    public async Task<LotDto> UpdateLotAsync(UpdateLotDto dto, int id, int userId)
    {
        ValidateLot(dto);

        var lot = await _repositoryWrapper.LotsRepository.GetFirstOrDefaultAsync(
            new QueryOptions<Lot>
            {
                Filter = item => item.Id == id,
                AsNoTracking = false
            });
        EnsureOwner(lot, userId);

        if (lot!.Status != LotStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft lot can be edited.");
        }

        await EnsureCategoryExistsAsync(dto.CategoryId);

        _mapper.Map(dto, lot);
        lot.CurrentPrice = dto.StartingPrice;
        lot.EndTime = NormalizeUtc(dto.EndTime);
        await _repositoryWrapper.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteLotAsync(int id, int userId)
    {
        // Lock the lot inside a transaction so a concurrent bid cannot slip
        // in between the funded check and the delete (cascade would strand
        // the bidder's deducted balance).
        await using var transaction = await _repositoryWrapper.BeginTransactionAsync();
        var lot = await _repositoryWrapper.LotsRepository.GetForUpdateAsync(id);
        EnsureOwner(lot, userId);

        var hasBids = await _repositoryWrapper.BidsRepository.AnyAsync(
            new QueryOptions<Bid>
            {
                Filter = bid => bid.LotId == id,
                AsNoTracking = true
            });
        if (hasBids)
        {
            throw new InvalidOperationException("A lot with bids cannot be deleted.");
        }

        _repositoryWrapper.LotsRepository.Delete(lot!);
        await _repositoryWrapper.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task<LotDto> CloseLotAsync(int id, int userId)
    {
        await using var transaction = await _repositoryWrapper.BeginTransactionAsync();
        var lot = await _repositoryWrapper.LotsRepository.GetForUpdateAsync(id);
        EnsureOwner(lot, userId);

        if (lot!.Status == LotStatus.Completed)
        {
            throw new InvalidOperationException("The lot is already completed.");
        }

        if (lot.Status != LotStatus.Active)
        {
            throw new InvalidOperationException("Only an active lot can be completed.");
        }

        if (DateTime.UtcNow < lot.EndTime)
        {
            throw new InvalidOperationException("The lot cannot be completed before its end time.");
        }

        var winningBid = await _repositoryWrapper.BidsRepository.GetHighestByLotIdAsync(id);
        if (winningBid is not null)
        {
            var seller = await _repositoryWrapper.UsersRepository.GetForUpdateAsync(lot.SellerId)
                ?? throw new KeyNotFoundException(
                    $"Seller with ID {lot.SellerId} not found.");

            if (seller.Balance > decimal.MaxValue - winningBid.Amount)
            {
                throw new InvalidOperationException(
                    "Seller balance exceeds the supported range.");
            }

            seller.Balance += winningBid.Amount;
            lot.WinnerId = winningBid.UserId;
        }

        lot.Status = LotStatus.Completed;
        await _repositoryWrapper.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetByIdAsync(id);
    }

    public async Task<int> CloseExpiredLotsAsync()
    {
        var expiredLots = await _repositoryWrapper.LotsRepository.GetAllAsync(
            new QueryOptions<Lot>
            {
                Filter = lot => lot.Status == LotStatus.Active && lot.EndTime <= DateTime.UtcNow,
                AsNoTracking = true
            });

        var closedCount = 0;
        foreach (var expired in expiredLots)
        {
            try
            {
                await using var transaction = await _repositoryWrapper.BeginTransactionAsync();
                var lot = await _repositoryWrapper.LotsRepository.GetForUpdateAsync(expired.Id);
                if (lot is null || lot.Status != LotStatus.Active || DateTime.UtcNow < lot.EndTime)
                {
                    continue;
                }

                var winningBid = await _repositoryWrapper.BidsRepository.GetHighestByLotIdAsync(lot.Id);
                if (winningBid is not null)
                {
                    var seller = await _repositoryWrapper.UsersRepository.GetForUpdateAsync(lot.SellerId);
                    if (seller is not null)
                    {
                        if (seller.Balance <= decimal.MaxValue - winningBid.Amount)
                        {
                            seller.Balance += winningBid.Amount;
                        }
                        lot.WinnerId = winningBid.UserId;
                    }
                }

                lot.Status = LotStatus.Completed;
                await _repositoryWrapper.SaveChangesAsync();
                await transaction.CommitAsync();
                closedCount++;
            }
            catch (Exception)
            {
                // Continue closing remaining lots
            }
        }

        return closedCount;
    }

    private async Task EnsureCategoryExistsAsync(int? categoryId)
    {
        if (!categoryId.HasValue)
        {
            return;
        }

        var exists = await _repositoryWrapper.CategoriesRepository.AnyAsync(
            new QueryOptions<Category>
            {
                Filter = category => category.Id == categoryId.Value,
                AsNoTracking = true
            });
        if (!exists)
        {
            throw new KeyNotFoundException(
                $"Category with ID {categoryId.Value} not found.");
        }
    }

    private static void ValidateLot(CreateLotDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new ArgumentException("Lot title is required.");
        }

        if (dto.Title.Trim().Length > 512)
        {
            throw new ArgumentException("Lot title cannot exceed 512 characters.");
        }

        if (dto.Description?.Length > 5000)
        {
            throw new ArgumentException("Lot description cannot exceed 5000 characters.");
        }

        if (dto.StartingPrice <= 0 || dto.StartingPrice > MonetaryLimits.MaxAmount ||
            dto.MinBidStep <= 0 || dto.MinBidStep > MonetaryLimits.MaxAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dto),
                $"Starting price and minimum bid step must be between 0.01 and {MonetaryLimits.MaxAmount}.");
        }

        if (!MonetaryLimits.HasValidScale(dto.StartingPrice) ||
            !MonetaryLimits.HasValidScale(dto.MinBidStep))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dto),
                "Starting price and minimum bid step cannot have more than two decimal places.");
        }

        if (NormalizeUtc(dto.EndTime) <= DateTime.UtcNow)
        {
            throw new ArgumentException("Lot end time must be in the future.");
        }

        if (!Enum.IsDefined(dto.Status) ||
            dto.Status is LotStatus.Completed or LotStatus.Cancelled)
        {
            throw new ArgumentException(
                "A lot can only be created or edited as draft or active.");
        }
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static void EnsureOwner(Lot? lot, int userId)
    {
        if (lot is null)
        {
            throw new KeyNotFoundException("Lot not found.");
        }

        if (lot.SellerId != userId)
        {
            throw new UnauthorizedAccessException(
                "Only the lot owner can modify or delete this lot.");
        }
    }
}
