namespace Auction.BLL.MapperProfiles;

using Auction.BLL.DTOs.Bids;
using Auction.DAL.Entities;
using AutoMapper;

public class BidProfile : Profile
{
    public BidProfile()
    {
        CreateMap<Bid, BidDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : null));
        CreateMap<Bid, UserBidDto>()
            .ForMember(dest => dest.LotTitle, opt => opt.MapFrom(src => src.Lot.Title))
            .ForMember(dest => dest.LotCurrentPrice, opt => opt.MapFrom(src => src.Lot.CurrentPrice))
            .ForMember(dest => dest.LotStatus, opt => opt.MapFrom(src => src.Lot.Status))
            .ForMember(dest => dest.LotEndTime, opt => opt.MapFrom(src => src.Lot.EndTime))
            .ForMember(dest => dest.LotMainImageUrl, opt => opt.MapFrom(src =>
                src.Lot.Images.FirstOrDefault(i => i.IsMain) != null
                    ? src.Lot.Images.FirstOrDefault(i => i.IsMain)!.Url
                    : src.Lot.Images.Select(i => i.Url).FirstOrDefault()));

        CreateMap<CreateBidDto, Bid>()
            .ForMember(bid => bid.Id, options => options.Ignore())
            .ForMember(bid => bid.UserId, options => options.Ignore())
            .ForMember(bid => bid.PlacedAt, options => options.Ignore());
    }
}
