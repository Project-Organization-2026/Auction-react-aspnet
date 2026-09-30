namespace Auction.BLL.MapperProfiles;

using Auction.BLL.DTOs.Bids;
using Auction.DAL.Entities;
using AutoMapper;

public class BidProfile : Profile
{
    public BidProfile()
    {
        CreateMap<Bid, BidDto>();
        CreateMap<CreateBidDto, Bid>()
            .ForMember(bid => bid.Id, options => options.Ignore())
            .ForMember(bid => bid.UserId, options => options.Ignore())
            .ForMember(bid => bid.PlacedAt, options => options.Ignore());
    }
}
