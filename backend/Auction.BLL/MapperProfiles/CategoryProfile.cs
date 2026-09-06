using Auction.BLL.DTOs.Categories;
using Auction.DAL.Entities;
using AutoMapper;

namespace Auction.BLL.MapperProfiles;

public class CategoryProfile : Profile
{
    public CategoryProfile()
    {
        CreateMap<Category, CategoryDto>();
    }
}
