namespace Auction.BLL.DTOs.Categories;

public class CreateCategoryDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.MaxLength(256)]
    public string Name { get; set; } = null!;

    [System.ComponentModel.DataAnnotations.MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
}
