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
    private readonly EthereumService? _ethereumService;

    public BidsService(
        IRepositoryWrapper repositoryWrapper,
        IMapper mapper,
        EthereumService? ethereumService = null)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
        _ethereumService = ethereumService;
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

        if (!MonetaryLimits.HasValidScale(dto.Amount))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dto),
                "Bid amount cannot have more than two decimal places.");
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
        if (previousBid?.UserId == userId && previousBid.Currency == BidCurrency.Usd)
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

        if (previousBid is not null && previousBid.UserId.HasValue && previousBid.UserId.Value != userId && previousBid.Currency == BidCurrency.Usd)
        {
            var previousBidder = lockedUsers[previousBid.UserId.Value];
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
        bid.User = bidder;
        bid.PlacedAt = DateTime.UtcNow;

        await _repositoryWrapper.BidsRepository.CreateAsync(bid);
        await _repositoryWrapper.SaveChangesAsync();
        await transaction.CommitAsync();

        return _mapper.Map<BidDto>(bid);
    }

    public async Task<BidDto> CreateOnChainBidAsync(CreateOnChainBidDto dto, int userId)
    {
        if (_ethereumService is null)
        {
            throw new InvalidOperationException("Ethereum service is not configured.");
        }

        if (userId <= 0)
        {
            throw new UnauthorizedAccessException("You must be logged in to place an ETH bid.");
        }

        var normalizedTxHash = dto.TxHash.Trim().ToLowerInvariant();
        var normalizedWallet = dto.WalletAddress.Trim().ToLowerInvariant();

        await using var transaction = await _repositoryWrapper.BeginTransactionAsync();

        var lot = await _repositoryWrapper.LotsRepository.GetForUpdateAsync(dto.LotId);
        if (lot is null)
        {
            throw new KeyNotFoundException($"Lot with ID {dto.LotId} not found.");
        }

        if (lot.Status != LotStatus.Active)
        {
            throw new InvalidOperationException("Bids can only be placed on active lots.");
        }

        if (lot.EndTime <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("This lot has already ended.");
        }

        if (lot.SellerId == userId)
        {
            throw new InvalidOperationException("The seller cannot place bids on their own lot.");
        }

        // Prevent duplicate registration of the same on-chain transaction
        var txExists = await _repositoryWrapper.BidsRepository.AnyAsync(new QueryOptions<Bid>
        {
            Filter = b => b.TxHash == normalizedTxHash,
            AsNoTracking = true
        });

        if (txExists)
        {
            throw new InvalidOperationException("This on-chain transaction has already been registered.");
        }

        // Verify on-chain transaction on Ethereum node
        await _ethereumService.VerifyTransactionAsync(
            normalizedTxHash,
            lot.ContractAddress,
            dto.AmountEth,
            normalizedWallet);

        var usdEquivalent = await _ethereumService.ConvertEthToUsdAsync(dto.AmountEth);

        var user = await _repositoryWrapper.UsersRepository.GetForUpdateAsync(userId);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        // Auto-link wallet address to user profile if not set yet
        if (string.IsNullOrEmpty(user.WalletAddress))
        {
            user.WalletAddress = normalizedWallet;
        }

        // Update lot prices & top bidder
        lot.CurrentPrice = usdEquivalent;
        lot.CurrentPriceEth = dto.AmountEth;
        lot.WinnerId = userId;

        var bid = new Bid
        {
            LotId = dto.LotId,
            UserId = userId,
            User = user,
            Amount = usdEquivalent,
            AmountEth = dto.AmountEth,
            Currency = BidCurrency.Eth,
            TxHash = normalizedTxHash,
            WalletAddress = normalizedWallet,
            PlacedAt = DateTime.UtcNow
        };

        // Note: refunds for outbid ETH users are handled by the smart contract
        // via pendingReturns[bidder] + withdraw(). No fiat balance changes needed.

        await _repositoryWrapper.BidsRepository.CreateAsync(bid);
        await _repositoryWrapper.SaveChangesAsync();
        await transaction.CommitAsync();

        var resultDto = _mapper.Map<BidDto>(bid);
        resultDto.UserName = user.UserName;

        return resultDto;
    }
}
