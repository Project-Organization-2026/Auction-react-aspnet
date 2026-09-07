using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Realizations.Base;
using Auction.DAL.Repositories.Interfaces.Users;
using Auction.DAL.Repositories.Options;

namespace Auction.DAL.Repositories.Realizations.Users;

public class UsersRepository : RepositoryBase<User>, IUsersRepository
{
    public UsersRepository(AuctionDbContext context) : base(context)
    {
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
