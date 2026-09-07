using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.Base;

namespace Auction.DAL.Repositories.Interfaces.Users;

public interface IUsersRepository : IRepositoryBase<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUserNameAsync(string userName);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByUserNameAsync(string userName);
}
