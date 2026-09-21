using Auction.BLL.DTOs.Categories;
using Auction.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auction.API.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CategoriesService _categoriesService;

    public CategoriesController(CategoriesService categoriesService)
    {
        _categoriesService = categoriesService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllCategories()
    {
        var categories = await _categoriesService.GetAllCategoriesAsync();
        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryById([FromRoute] int id)
    {
        try
        {
            return Ok(await _categoriesService.GetCategoryByIdAsync(id));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        try
        {
            var category = await _categoriesService.CreateCategoryAsync(dto);
            return StatusCode(StatusCodes.Status201Created, category);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(
        [FromRoute] int id,
        [FromBody] UpdateCategoryDto dto)
    {
        try
        {
            var category = await _categoriesService.UpdateCategoryAsync(dto, id);
            return Ok(category);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory([FromRoute] int id)
    {
        try
        {
            await _categoriesService.DeleteCategoryAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
