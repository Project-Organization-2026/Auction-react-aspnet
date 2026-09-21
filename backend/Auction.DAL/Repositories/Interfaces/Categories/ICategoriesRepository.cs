using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces.Base;

namespace Auction.DAL.Repositories.Interfaces.Categories;

public interface ICategoriesRepository : IRepositoryBase<Category>
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
    Task<Category?> GetCategoryByIdAsync(int id);
    Task CreateCategoryAsync(Category category);
    Task UpdateCategoryAsync(Category category);
    Task DeleteCategoryAsync(Category category);

}
