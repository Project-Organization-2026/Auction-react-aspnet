using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.Base;

namespace Auction.DAL.Repositories.Interfaces.Lots;

public interface ILotsRepository : IRepositoryBase<Lot>
{
    /// <summary>Locks a lot until the current transaction ends and returns its latest values.</summary>
    Task<Lot?> GetForUpdateAsync(int lotId);

    /// <summary>Locks a lot and loads its images until the current transaction ends.</summary>
    Task<Lot?> GetForUpdateWithImagesAsync(int lotId);
}
