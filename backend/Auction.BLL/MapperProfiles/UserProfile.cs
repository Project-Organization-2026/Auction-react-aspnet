using Auction.BLL.DTOs.Users;
using Auction.DAL.Entities;
using AutoMapper;

namespace Auction.BLL.MapperProfiles;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserSummaryDto>();
        CreateMap<User, UserProfileDto>();
        CreateMap<UpdateUserProfileDto, User>()
            .ForMember(user => user.UserName,
                options => options.MapFrom(dto => dto.UserName.Trim()))
            .ForMember(user => user.Email,
                options => options.MapFrom(dto => dto.Email.Trim().ToLowerInvariant()));
    }
}
