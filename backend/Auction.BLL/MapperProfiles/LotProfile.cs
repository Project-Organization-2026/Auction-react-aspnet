using Auction.BLL.DTOs.Lots;
using Auction.BLL.DTOs.LotImages;
using Auction.DAL.Entities;
using AutoMapper;

namespace Auction.BLL.MapperProfiles;

public class LotProfile : Profile
{
    public LotProfile()
    {
        CreateMap<Lot, LotDto>();
        CreateMap<CreateLotDto, Lot>();
        CreateMap<UpdateLotDto, Lot>();
        CreateMap<LotImage, LotImageDto>();
        CreateMap<AddLotImageDto, LotImage>();
    }
}
