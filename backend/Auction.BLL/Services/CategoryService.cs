
using Auction.BLL.DTOs.Categories;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces;
using AutoMapper;

public class CategoriesService
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;

    public CategoriesService(IRepositoryWrapper repositoryWrapper, IMapper mapper) => (_repositoryWrapper, _mapper) = (repositoryWrapper, mapper);



    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync()
    {
        var categories = await _repositoryWrapper.CategoriesRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<CategoryDto>>(categories);
    }
    public async Task<CategoryDto> GetCategoryByIdAsync(int id)
    {
        var category = await _repositoryWrapper.CategoriesRepository.GetCategoryByIdAsync(id);
        return _mapper.Map<CategoryDto>(category);
    }
    public async Task CreateCategoryAsync(CreateCategoryDto dto)
    {
        var category = _mapper.Map<Category>(dto);
        await _repositoryWrapper.CategoriesRepository.CreateCategoryAsync(category);
    }
    public async Task UpdateCategoryAsync(UpdateCategoryDto dto, int id)
    {
        var category = await _repositoryWrapper.CategoriesRepository.GetCategoryByIdAsync(id);
        _mapper.Map(dto, category);
        await _repositoryWrapper.CategoriesRepository.UpdateCategoryAsync(category);
    }
    public async Task DeleteCategoryAsync(int id)
    {
        var category = await _repositoryWrapper.CategoriesRepository.GetCategoryByIdAsync(id);
        await _repositoryWrapper.CategoriesRepository.DeleteCategoryAsync(category);
    }
}