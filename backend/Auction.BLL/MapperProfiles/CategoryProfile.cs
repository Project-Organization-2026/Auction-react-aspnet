using Auction.BLL.DTOs.Categories;
using Auction.DAL.Entities;
using AutoMapper;

namespace Auction.BLL.MapperProfiles;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();
        CreateMap<CreateCategoryDto, Category>()
            .ForMember(category => category.Name,
                options => options.MapFrom(dto => dto.Name.Trim()));
        CreateMap<UpdateCategoryDto, Category>()
            .ForMember(category => category.Name,
                options => options.MapFrom(dto => dto.Name.Trim()));
    }
}
