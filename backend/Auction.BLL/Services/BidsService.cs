// TODO: Implement BidsService.
// Required operations:
// - GetBidsByLotIdAsync(int lotId): query BidsRepository by LotId, order by newest first,
//   and map the result to IEnumerable<BidDto>.
// - CreateBidAsync(CreateBidDto dto, int userId): load the lot, verify it exists and
//   DateTime.UtcNow < lot.EndTime, verify dto.Amount > lot.CurrentPrice, set the new
//   current price, create the bid with userId, save both changes, and return BidDto.
// Consider ArgumentException/InvalidOperationException conventions used by LotsService.

namespace Auction.BLL.Services;
using Auction.BLL.DTOs.Bids;
using Auction.BLL.DTOs.Common;
using Auction.DAL.Repositories.Interfaces;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;
using Auction.DAL.Repositories.Options;
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
    public async Task<BidDto> CreateBidAsync(CreateBidDto dto, int userId)
    {
        var lot = await _repositoryWrapper.LotsRepository.GetFirstOrDefaultAsync(new QueryOptions<Lot>
        {
            Filter = l => l.Id == dto.LotId,
            AsNoTracking = false,
        });

        if (lot == null)
        {
            throw new ArgumentException($"Lot with ID {dto.LotId} not found.");
        }

        if (DateTime.UtcNow >= lot.EndTime)
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
        return _mapper.Map<BidDto>(bid);
    }
}
