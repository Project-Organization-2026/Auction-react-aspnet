using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.Bids;
using Auction.DAL.Repositories.Realizations.Base;
using Microsoft.EntityFrameworkCore;

namespace Auction.DAL.Repositories.Realizations.Bids;

public class BidsRepository : RepositoryBase<Bid>, IBidsRepository
{
    private readonly AuctionDbContext _context;

    public BidsRepository(AuctionDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Returns a stable newest-first page, using the ID to break timestamp ties.</summary>
    public async Task<(IReadOnlyList<Bid> Items, int TotalCount)> GetByLotIdAsync(
        int lotId,
        int page,
        int pageSize)
    {
        var query = _context.Bids
            .AsNoTracking()
            .Where(bid => bid.LotId == lotId);

        var totalCount = await query.CountAsync();
        var offset = (long)(page - 1) * pageSize;

        if (offset > int.MaxValue)
        {
            return (Array.Empty<Bid>(), totalCount);
        }

        var items = await query
            .OrderByDescending(bid => bid.PlacedAt)
            .ThenByDescending(bid => bid.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
