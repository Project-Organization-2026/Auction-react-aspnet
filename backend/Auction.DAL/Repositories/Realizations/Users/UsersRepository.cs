using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Realizations.Base;
using Auction.DAL.Repositories.Interfaces.Users;

namespace Auction.DAL.Repositories.Realizations.Users;

public class UsersRepository : RepositoryBase<User>, IUsersRepository
{
    public UsersRepository(AuctionDbContext context) : base(context)
    {
    }
}
