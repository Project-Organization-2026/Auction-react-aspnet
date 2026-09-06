using Auction.DAL.Repositories.Options;

namespace Auction.DAL.Repositories.Interfaces.Base;

public interface IRepositoryBase<T>
    where T : class
{
    Task<T?> GetFirstOrDefaultAsync(QueryOptions<T>? options = null);
    Task<IEnumerable<T>> GetAllAsync(QueryOptions<T>? options = null);
    Task<T> CreateAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
}
