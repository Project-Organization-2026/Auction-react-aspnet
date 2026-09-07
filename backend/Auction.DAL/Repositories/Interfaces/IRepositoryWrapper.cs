using Auction.DAL.Data;
using Auction.DAL.Repositories.Interfaces.Base;
using Auction.DAL.Repositories.Interfaces.Categories;
using Auction.DAL.Repositories.Interfaces.Bids;
using Auction.DAL.Repositories.Interfaces.LotImages;
using Auction.DAL.Repositories.Interfaces.Lots;
using Auction.DAL.Repositories.Interfaces.Users;

namespace Auction.DAL.Repositories.Interfaces;

public interface IRepositoryWrapper
{
    IBidsRepository BidsRepository { get; }
    ILotsRepository LotsRepository { get; }
    ICategoriesRepository CategoriesRepository { get; }
    ILotImagesRepository LotImagesRepository { get; }
    IUsersRepository UsersRepository { get; }

    IRepositoryBase<TEntity> GetRepository<TEntity>()
        where TEntity : class;

    Task<int> SaveChangesAsync();
}
