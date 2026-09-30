using Auction.BLL.DTOs.Categories;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Options;
using AutoMapper;

namespace Auction.BLL.Services;

public class CategoriesService
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;

    public CategoriesService(IRepositoryWrapper repositoryWrapper, IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _repositoryWrapper.CategoriesRepository.GetAllAsync(
            new QueryOptions<Category>
            {
                AsNoTracking = true,
                OrderByASC = category => category.Name
            });

        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(int id)
    {
        var category = await FindCategoryAsync(id, asNoTracking: true);
        if (category is null)
        {
            throw new KeyNotFoundException($"Category with ID {id} not found.");
        }

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto dto)
    {
        ValidateName(dto.Name);

        var category = _mapper.Map<Category>(dto);
        await _repositoryWrapper.CategoriesRepository.CreateAsync(category);
        await _repositoryWrapper.SaveChangesAsync();

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task<CategoryDto> UpdateCategoryAsync(UpdateCategoryDto dto, int id)
    {
        ValidateName(dto.Name);

        var category = await FindCategoryAsync(id, asNoTracking: false);
        if (category is null)
        {
            throw new KeyNotFoundException($"Category with ID {id} not found.");
        }

        _mapper.Map(dto, category);
        await _repositoryWrapper.SaveChangesAsync();

        return _mapper.Map<CategoryDto>(category);
    }

    public async Task DeleteCategoryAsync(int id)
    {
        var category = await FindCategoryAsync(id, asNoTracking: false);
        if (category is null)
        {
            throw new KeyNotFoundException($"Category with ID {id} not found.");
        }

        _repositoryWrapper.CategoriesRepository.Delete(category);
        await _repositoryWrapper.SaveChangesAsync();
    }

    private async Task<Category?> FindCategoryAsync(int id, bool asNoTracking)
    {
        return await _repositoryWrapper.CategoriesRepository.GetFirstOrDefaultAsync(
            new QueryOptions<Category>
            {
                Filter = category => category.Id == id,
                AsNoTracking = asNoTracking
            });
    }

    private static void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.");
        }
    }
}
