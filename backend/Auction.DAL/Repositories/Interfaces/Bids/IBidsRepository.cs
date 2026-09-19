using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.Base;

namespace Auction.DAL.Repositories.Interfaces.Bids;

public interface IBidsRepository : IRepositoryBase<Bid>
{
    Task<(IReadOnlyList<Bid> Items, int TotalCount)> GetByLotIdAsync(
        int lotId,
        int page,
        int pageSize);
}
