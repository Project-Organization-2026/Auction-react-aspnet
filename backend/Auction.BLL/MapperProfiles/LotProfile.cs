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
        CreateMap<CreateLotDto, Lot>()
            .ForMember(lot => lot.Title,
                options => options.MapFrom(dto => dto.Title.Trim()))
            .ForMember(lot => lot.Description,
                options => options.MapFrom(dto => (dto.Description ?? string.Empty).Trim()))
            .ForMember(lot => lot.Id, options => options.Ignore())
            .ForMember(lot => lot.SellerId, options => options.Ignore())
            .ForMember(lot => lot.CurrentPrice, options => options.Ignore())
            .ForMember(lot => lot.StartTime, options => options.Ignore())
            .ForMember(lot => lot.CreatedAt, options => options.Ignore())
            .ForMember(lot => lot.WinnerId, options => options.Ignore());
        CreateMap<UpdateLotDto, Lot>().IncludeBase<CreateLotDto, Lot>();
        CreateMap<LotImage, LotImageDto>();
        CreateMap<AddLotImageDto, LotImage>();
    }
}
