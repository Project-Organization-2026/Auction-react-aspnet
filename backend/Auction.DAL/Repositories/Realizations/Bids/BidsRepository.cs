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

    public async Task<IEnumerable<Bid>> GetByLotIdAsync(int lotId)
    {
        return await _context.Bids
            .AsNoTracking()
            .Where(bid => bid.LotId == lotId)
            .OrderByDescending(bid => bid.PlacedAt)
            .ToListAsync();
    }
}
