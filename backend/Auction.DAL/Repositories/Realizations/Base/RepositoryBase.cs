using Auction.DAL.Data;
using Auction.DAL.Repositories.Interfaces.Base;
using Auction.DAL.Repositories.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Auction.DAL.Repositories.Realizations.Base;

public class RepositoryBase<T> : IRepositoryBase<T>
    where T : class
{
    private readonly AuctionDbContext _context;

    public RepositoryBase(AuctionDbContext context)
    {
        _context = context;
    }

    public async Task<T> CreateAsync(T entity)
    {
        return (await _context.Set<T>().AddAsync(entity)).Entity;
    }

    public void Delete(T entity)
    {
        _context.Set<T>().Remove(entity);
    }

    public async Task<IEnumerable<T>> GetAllAsync(QueryOptions<T>? options = null)
    {
        IQueryable<T> query = _context.Set<T>();
        if (options != null)
        {
            query = ApplyQueryOptions(query, options);
        }

        return await query.ToListAsync();
    }

    public async Task<T?> GetFirstOrDefaultAsync(QueryOptions<T>? options = null)
    {
        IQueryable<T> query = _context.Set<T>();
        if (options != null)
        {
            query = ApplyQueryOptions(query, options);
        }

        return await query.FirstOrDefaultAsync();
    }

    public void Update(T entity)
    {
        _context.Set<T>().Update(entity);
    }

    private static IQueryable<T> ApplyAsNoTracking(IQueryable<T> query, bool asNoTracking)
    {
        return asNoTracking ? query.AsNoTracking() : query;
    }

    private static IQueryable<T> ApplySplitQuery(IQueryable<T> query, bool asSplitQuery)
    {
        return asSplitQuery ? query.AsSplitQuery() : query;
    }

    private static IQueryable<T> ApplyInclude(IQueryable<T> query, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include)
    {
        return include != null ? include(query) : query;
    }

    private static IQueryable<T> ApplyFilter(IQueryable<T> query, Expression<Func<T, bool>>? filter)
    {
        return filter != null ? query.Where(filter) : query;
    }

    private static IQueryable<T> ApplyOrderBy(IQueryable<T> query, Expression<Func<T, object>>? orderByASC, Expression<Func<T, object>>? orderByDESC)
    {
        if (orderByASC != null)
        {
            query = query.OrderBy(orderByASC);
        }
        else if (orderByDESC != null)
        {
            query = query.OrderByDescending(orderByDESC);
        }
        return query;
    }

    private static IQueryable<T> ApplyQueryOptions(IQueryable<T> query, QueryOptions<T> options)
    {
        query = ApplyAsNoTracking(query, options.AsNoTracking);
        query = ApplySplitQuery(query, options.AsSplitQuery);
        query = ApplyInclude(query, options.Include);
        query = ApplyFilter(query, options.Filter);
        query = ApplyOrderBy(query, options.OrderByASC, options.OrderByDESC);
        return query;
    }
}
