using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.LotImages;
using Auction.DAL.Repositories.Realizations.Base;

namespace Auction.DAL.Repositories.Realizations.LotImages;

public class LotImagesRepository : RepositoryBase<LotImage>, ILotImagesRepository
{
    public LotImagesRepository(AuctionDbContext context) : base(context)
    {
    }
}
