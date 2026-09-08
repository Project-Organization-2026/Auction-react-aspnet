using Auction.DAL.Data;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Interfaces.Base;
using Auction.DAL.Repositories.Interfaces.Bids;
using Auction.DAL.Repositories.Interfaces.Categories;
using Auction.DAL.Repositories.Interfaces.LotImages;
using Auction.DAL.Repositories.Interfaces.Lots;
using Auction.DAL.Repositories.Interfaces.Users;
using Auction.DAL.Repositories.Realizations.Bids;
using Auction.DAL.Repositories.Realizations.Categories;
using Auction.DAL.Repositories.Realizations.LotImages;
using Auction.DAL.Repositories.Realizations.Lots;
using Auction.DAL.Repositories.Realizations.Users;
using System.Reflection;

namespace Auction.DAL.Repositories.Realizations;

public class RepositoryWrapper : IRepositoryWrapper
{
    private readonly AuctionDbContext _context;

    private IBidsRepository? _bidsRepository;
    private ILotsRepository? _lotsRepository;
    private ILotImagesRepository? _lotImagesRepository;
    private ICategoriesRepository? _categoryRepository;
    private IUsersRepository? _usersRepository;

    public IBidsRepository BidsRepository => _bidsRepository
        ??= new BidsRepository(_context);
    public ILotsRepository LotsRepository => _lotsRepository
        ??= new LotsRepository(_context);
    public ILotImagesRepository LotImagesRepository => _lotImagesRepository
        ??= new LotImagesRepository(_context);
    public ICategoriesRepository CategoriesRepository => _categoryRepository
        ??= new CategoriesRepository(_context);
    public IUsersRepository UsersRepository => _usersRepository
        ??= new UsersRepository(_context);

    public RepositoryWrapper(AuctionDbContext context)
    {
        _context = context;
    }

    public IRepositoryBase<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var propertyValue = property.GetValue(this);

            if (propertyValue is IRepositoryBase<TEntity> matchingRepository)
            {
                return matchingRepository;
            }
        }

        throw new NotImplementedException(
            $"Repository for entity type '{typeof(TEntity).Name}' is not found in {nameof(RepositoryWrapper)}.");
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
