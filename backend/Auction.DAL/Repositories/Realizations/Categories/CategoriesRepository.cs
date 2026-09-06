using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.Categories;
using Auction.DAL.Repositories.Realizations.Base;

namespace Auction.DAL.Repositories.Realizations.Categories;

public class CategoriesRepository : RepositoryBase<Category>, ICategoriesRepository
{
    public CategoriesRepository(AuctionDbContext context) : base(context)
    {
    }
}
