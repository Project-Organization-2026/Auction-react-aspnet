using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.Base;

namespace Auction.DAL.Repositories.Interfaces.Lots;

public interface ILotsRepository : IRepositoryBase<Lot>
{
    Task<(IReadOnlyList<Lot> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        int? categoryId,
        Auction.DAL.Enums.LotStatus? status);

    Task<Lot?> GetDetailsByIdAsync(int lotId, bool asNoTracking = true);

    /// <summary>Locks a lot until the current transaction ends and returns its latest values.</summary>
    Task<Lot?> GetForUpdateAsync(int lotId);

    /// <summary>Locks a lot and loads its images until the current transaction ends.</summary>
    Task<Lot?> GetForUpdateWithImagesAsync(int lotId);

    Task<IReadOnlyList<Lot>> GetWonLotsByUserIdAsync(int userId);
}
