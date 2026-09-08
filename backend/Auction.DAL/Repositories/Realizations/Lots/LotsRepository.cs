using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.Lots;
using Auction.DAL.Repositories.Realizations.Base;

namespace Auction.DAL.Repositories.Realizations.Lots;

public class LotsRepository : RepositoryBase<Lot>, ILotsRepository
{
    public LotsRepository(AuctionDbContext context) : base(context)
    {
    }
}
