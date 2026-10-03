using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Realizations.Base;
using Auction.DAL.Repositories.Interfaces.Users;
using Auction.DAL.Repositories.Options;
using Microsoft.EntityFrameworkCore;

namespace Auction.DAL.Repositories.Realizations.Users;

public class UsersRepository : RepositoryBase<User>, IUsersRepository
{
    private readonly AuctionDbContext _context;

    public UsersRepository(AuctionDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<User?> GetProfileByIdAsync(int userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Include(user => user.CreatedLots)
                .ThenInclude(lot => lot.Winner)
            .Include(user => user.CreatedLots)
                .ThenInclude(lot => lot.Category)
            .Include(user => user.CreatedLots)
                .ThenInclude(lot => lot.Images)
            .SingleOrDefaultAsync(user => user.Id == userId);

        if (user != null)
        {
            foreach (var lot in user.CreatedLots)
            {
                lot.Seller = user;
            }
        }

        return user;
    }

    public Task<User?> GetForUpdateAsync(int userId)
    {
        if (_context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("A transaction is required to lock a user.");
        }

        return _context.Users
            .FromSqlInterpolated($"SELECT * FROM \"Users\" WHERE \"Id\" = {userId} FOR UPDATE")
            .AsTracking()
            .SingleOrDefaultAsync();
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        return GetFirstOrDefaultAsync(new QueryOptions<User>
        {
            Filter = user => user.Email == email,
            AsNoTracking = true
        });
    }

    public Task<User?> GetByUserNameAsync(string userName)
    {
        return GetFirstOrDefaultAsync(new QueryOptions<User>
        {
            Filter = user => user.UserName == userName,
            AsNoTracking = true
        });
    }

    public Task<bool> ExistsByEmailAsync(string email)
    {
        return AnyAsync(new QueryOptions<User>
        {
            Filter = user => user.Email == email,
            AsNoTracking = true
        });
    }

    public Task<bool> ExistsByUserNameAsync(string userName)
    {
        return AnyAsync(new QueryOptions<User>
        {
            Filter = user => user.UserName == userName,
            AsNoTracking = true
        });
    }
}
