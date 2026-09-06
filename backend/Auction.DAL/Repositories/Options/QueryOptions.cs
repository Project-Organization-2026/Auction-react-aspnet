using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Auction.DAL.Repositories.Options;

public record QueryOptions<T>
{
    public bool AsNoTracking { get; set; }
    public bool AsSplitQuery { get; set; }
    public Func<IQueryable<T>, IIncludableQueryable<T, object>>? Include { get; set; }
    public Expression<Func<T, bool>>? Filter { get; set; }
    public Expression<Func<T, object>>? OrderByASC { get; set; }
    public Expression<Func<T, object>>? OrderByDESC { get; set; }
}
