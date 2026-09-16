// TODO: Create an AutoMapper Profile for Bid <-> BidDto and CreateBidDto -> Bid.
// Keep server-owned values (Id, UserId, PlacedAt) controlled by the service.

namespace Auction.BLL.MapperProfiles;

using Auction.BLL.DTOs.Bids;
using Auction.DAL.Entities;
using AutoMapper;

public class BidProfile : Profile
{
    public BidProfile()
    {
        CreateMap<Bid, BidDto>();
        CreateMap<CreateBidDto, Bid>();
    }
}