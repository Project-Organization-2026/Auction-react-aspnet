namespace Auction.BLL.Services;
using Auction.BLL.DTOs.Bids;
using Auction.BLL.DTOs.Common;
using Auction.DAL.Repositories.Interfaces;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using Auction.DAL.Enums;
using Auction.DAL.Entities;

public class BidsService
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly AutoMapper.IMapper _mapper;
    public BidsService(IRepositoryWrapper repositoryWrapper, IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
    }

    /// <summary>Returns newest bids with bounded page and page-size values.</summary>
    public async Task<PagedResultDto<BidDto>> GetBidsByLotIdAsync(
        int lotId,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

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
    /// <summary>Validates a locked active lot and commits its price and bid atomically.</summary>
    public async Task<BidDto> CreateBidAsync(CreateBidDto dto, int userId)
    {
        await using var transaction = await _repositoryWrapper.BeginTransactionAsync();
        var lot = await _repositoryWrapper.LotsRepository.GetForUpdateAsync(dto.LotId);

        if (lot == null)
        {
            throw new ArgumentException($"Lot with ID {dto.LotId} not found.");
        }

        if (lot.Status != LotStatus.Active || DateTime.UtcNow >= lot.EndTime)
        {
            throw new InvalidOperationException($"Lot with ID {dto.LotId} is closed.");
        }

        if (dto.Amount <= lot.CurrentPrice)
        {
            throw new InvalidOperationException("Bid amount must be greater than current price.");
        }

        lot.CurrentPrice = dto.Amount;
        var bid = _mapper.Map<Bid>(dto);
        bid.UserId = userId;
        await _repositoryWrapper.BidsRepository.CreateAsync(bid);
        await _repositoryWrapper.SaveChangesAsync();
        await transaction.CommitAsync();
        return _mapper.Map<BidDto>(bid);
    }
}
