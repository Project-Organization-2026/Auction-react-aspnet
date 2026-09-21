using Auction.BLL.DTOs.Categories;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CategoriesService _categoriesService;

    public CategoriesController(CategoriesService categoriesService) => _categoriesService = categoriesService;

    [HttpGet]
    public async Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync() => await _categoriesService.GetAllCategoriesAsync();

    [HttpGet("{id:int}")]
    public async Task<CategoryDto> GetCategoryByIdAsync([FromRoute] int id) => await _categoriesService.GetCategoryByIdAsync(id);

    [HttpPost]
    public async Task<IActionResult> CreateCategoryAsync([FromBody] CreateCategoryDto dto)
    {
        await _categoriesService.CreateCategoryAsync(dto);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCategoryAsync([FromRoute] int id, [FromBody] UpdateCategoryDto dto)
    {
        await _categoriesService.UpdateCategoryAsync(dto, id);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCategoryAsync([FromRoute] int id)
    {
        await _categoriesService.DeleteCategoryAsync(id);
        return NoContent();
    }
}