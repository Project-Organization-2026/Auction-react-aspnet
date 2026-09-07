using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.Bids;
using Auction.DAL.Repositories.Realizations.Base;

namespace Auction.DAL.Repositories.Realizations.Bids;

public class BidsRepository : RepositoryBase<Bid>, IBidsRepository
{
    public BidsRepository(AuctionDbContext context) : base(context)
    {
    }
}
