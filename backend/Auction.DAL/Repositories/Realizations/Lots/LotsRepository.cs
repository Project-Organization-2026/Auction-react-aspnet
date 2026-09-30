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

    public async Task<(IReadOnlyList<Lot> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        int? categoryId,
        Auction.DAL.Enums.LotStatus? status)
    {
        var query = _context.Lots.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(lot =>
                EF.Functions.ILike(lot.Title, $"%{term}%") ||
                EF.Functions.ILike(lot.Description, $"%{term}%"));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(lot => lot.CategoryId == categoryId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(lot => lot.Status == status.Value);
        }

        var totalCount = await query.CountAsync();
        var offset = (long)(page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            return (Array.Empty<Lot>(), totalCount);
        }

        var items = await AddDetails(query)
            .OrderByDescending(lot => lot.CreatedAt)
            .ThenByDescending(lot => lot.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<Lot?> GetDetailsByIdAsync(int lotId, bool asNoTracking = true)
    {
        IQueryable<Lot> query = _context.Lots;
        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return AddDetails(query)
            .AsSplitQuery()
            .SingleOrDefaultAsync(lot => lot.Id == lotId);
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

    public async Task<Lot?> GetForUpdateWithImagesAsync(int lotId)
    {
        if (_context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("A transaction is required to lock a lot.");

        return await _context.Lots
            .FromSqlInterpolated($"SELECT * FROM \"Lots\" WHERE \"Id\" = {lotId} FOR UPDATE")
            .Include(lot => lot.Images)
            .AsTracking()
            .SingleOrDefaultAsync();
    }

    private static IQueryable<Lot> AddDetails(IQueryable<Lot> query)
    {
        return query
            .Include(lot => lot.Seller)
            .Include(lot => lot.Winner)
            .Include(lot => lot.Category)
            .Include(lot => lot.Images)
            .Include(lot => lot.Bids);
    }
}
