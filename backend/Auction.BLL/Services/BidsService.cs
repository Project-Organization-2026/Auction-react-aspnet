using Auction.BLL.Constants;
using Auction.BLL.DTOs.Bids;
using Auction.BLL.DTOs.Common;
using Auction.DAL.Entities;
using Auction.DAL.Enums;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Options;
using AutoMapper;

namespace Auction.BLL.Services;

public class BidsService
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;

    public BidsService(IRepositoryWrapper repositoryWrapper, IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<BidDto>> GetBidsByLotIdAsync(
        int lotId,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var lotExists = await _repositoryWrapper.LotsRepository.AnyAsync(
            new QueryOptions<Lot>
            {
                Filter = lot => lot.Id == lotId,
                AsNoTracking = true
            });
        if (!lotExists)
        {
            throw new KeyNotFoundException($"Lot with ID {lotId} not found.");
        }

        var result = await _repositoryWrapper.BidsRepository
            .GetByLotIdAsync(lotId, page, pageSize);

        return new PagedResultDto<BidDto>
        {
            Items = _mapper.Map<IReadOnlyList<BidDto>>(result.Items),
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount
        };
    }

    public async Task<BidDto> CreateBidAsync(CreateBidDto dto, int userId)
    {
        if (dto.Amount <= 0 || dto.Amount > MonetaryLimits.MaxAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dto),
                $"Bid amount must be between 0.01 and {MonetaryLimits.MaxAmount}.");
        }

        await using var transaction = await _repositoryWrapper.BeginTransactionAsync();
        var lot = await _repositoryWrapper.LotsRepository.GetForUpdateAsync(dto.LotId);

        if (lot is null)
        {
            throw new KeyNotFoundException($"Lot with ID {dto.LotId} not found.");
        }

        if (lot.Status != LotStatus.Active || DateTime.UtcNow >= lot.EndTime)
        {
            throw new InvalidOperationException($"Lot with ID {dto.LotId} is closed.");
        }

        if (lot.SellerId == userId)
        {
            throw new InvalidOperationException("A seller cannot bid on their own lot.");
        }

        if (dto.Amount <= lot.CurrentPrice ||
            dto.Amount - lot.CurrentPrice < lot.MinBidStep)
        {
            throw new InvalidOperationException(
                $"Bid must exceed the current price by at least {lot.MinBidStep}.");
        }

        var previousBid = await _repositoryWrapper.BidsRepository
            .GetHighestByLotIdAsync(dto.LotId);

        var userIdsToLock = new[] { userId, previousBid?.UserId }
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToArray();

        var lockedUsers = new Dictionary<int, User>();
        foreach (var id in userIdsToLock)
        {
            var user = await _repositoryWrapper.UsersRepository.GetForUpdateAsync(id);
            if (user is null)
            {
                throw new KeyNotFoundException($"User with ID {id} not found.");
            }

            lockedUsers[id] = user;
        }

        var bidder = lockedUsers[userId];
        var availableBalance = bidder.Balance;
        if (previousBid?.UserId == userId)
        {
            if (availableBalance > decimal.MaxValue - previousBid.Amount)
            {
                throw new InvalidOperationException("Bidder balance exceeds the supported range.");
            }

            availableBalance += previousBid.Amount;
        }

        if (availableBalance < dto.Amount)
        {
            throw new InvalidOperationException("Insufficient balance for this bid.");
        }

        if (previousBid is not null && previousBid.UserId != userId)
        {
            var previousBidder = lockedUsers[previousBid.UserId];
            if (previousBidder.Balance > decimal.MaxValue - previousBid.Amount)
            {
                throw new InvalidOperationException(
                    "Previous bidder balance exceeds the supported range.");
            }

            previousBidder.Balance += previousBid.Amount;
        }

        bidder.Balance = availableBalance - dto.Amount;
        lot.CurrentPrice = dto.Amount;
        lot.WinnerId = userId;

        var bid = _mapper.Map<Bid>(dto);
        bid.UserId = userId;
        bid.PlacedAt = DateTime.UtcNow;

        await _repositoryWrapper.BidsRepository.CreateAsync(bid);
        await _repositoryWrapper.SaveChangesAsync();
        await transaction.CommitAsync();

        return _mapper.Map<BidDto>(bid);
    }
}
