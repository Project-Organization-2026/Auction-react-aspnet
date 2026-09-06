using Auction.BLL.DTOs.Users;
using Auction.DAL.Entities;
using AutoMapper;

namespace Auction.BLL.MapperProfiles;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserSummaryDto>();
    }
}
