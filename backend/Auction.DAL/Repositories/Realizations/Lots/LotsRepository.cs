using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.Lots;
using Auction.DAL.Repositories.Realizations.Base;
using Microsoft.EntityFrameworkCore;

namespace Auction.DAL.Repositories.Realizations.Lots;

public class LotsRepository : RepositoryBase<Lot>, ILotsRepository
{
    private readonly AuctionDbContext _context;

    public LotsRepository(AuctionDbContext context) : base(context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Lot?> GetForUpdateAsync(int lotId)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A transaction is required to lock a lot.");

        var lot = await _context.Lots
            .FromSqlInterpolated($"SELECT * FROM \"Lots\" WHERE \"Id\" = {lotId} FOR UPDATE")
            .AsTracking()
            .SingleOrDefaultAsync();

        // Refresh any entity already tracked before the lock was acquired.
        if (lot is not null)
            await _context.Entry(lot).ReloadAsync();

        return lot;
    }

    public async Task<IEnumerable<Lot>> GetLotsBySellerIdAsync(int sellerId)
    {
        return await _context.Lots
            .Where(lot => lot.SellerId == sellerId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Lot>> GetLotByIdAsync(int ID)
    {
        return await _context.Lots
            .Where(lot => lot.Id == ID)
            .ToListAsync();
    }

}
