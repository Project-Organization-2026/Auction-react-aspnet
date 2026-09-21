using Auction.BLL.DTOs.LotImages;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Options;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Auction.BLL.Services;

public class LotImagesService
{
    private readonly IRepositoryWrapper _repositoryWrapper;
    private readonly IMapper _mapper;

    public LotImagesService(IRepositoryWrapper repositoryWrapper, IMapper mapper)
    {
        _repositoryWrapper = repositoryWrapper;
        _mapper = mapper;
    }

    public async Task<LotImageDto> AddImageToLotAsync(
        int lotId,
        AddLotImageDto dto,
        int userId)
    {
        var lot = await GetLotWithImagesAsync(lotId);
        EnsureLotOwner(lot, userId, "add images");

        if (string.IsNullOrWhiteSpace(dto.Url))
        {
            throw new ArgumentException("Image URL is required.");
        }

        if (dto.IsMain)
        {
            foreach (var image in lot!.Images)
            {
                image.IsMain = false;
            }
        }

        var imageToCreate = _mapper.Map<LotImage>(dto);
        imageToCreate.LotId = lotId;

        await _repositoryWrapper.LotImagesRepository.CreateAsync(imageToCreate);
        await _repositoryWrapper.SaveChangesAsync();

        return _mapper.Map<LotImageDto>(imageToCreate);
    }

    public async Task DeleteImageAsync(int imageId, int userId)
    {
        var image = await _repositoryWrapper.LotImagesRepository.GetFirstOrDefaultAsync(
            new QueryOptions<LotImage>
            {
                Filter = item => item.Id == imageId,
                Include = query => query.Include(item => item.Lot),
                AsNoTracking = false
            });

        if (image is null)
        {
            throw new ArgumentException($"Lot image with ID {imageId} not found.");
        }

        EnsureLotOwner(image.Lot, userId, "delete this image");
        _repositoryWrapper.LotImagesRepository.Delete(image);
        await _repositoryWrapper.SaveChangesAsync();
    }

    public async Task<LotImageDto> SetMainImageAsync(
        int imageId,
        int lotId,
        int userId)
    {
        var lot = await GetLotWithImagesAsync(lotId);
        EnsureLotOwner(lot, userId, "set the main image");

        var imageToSet = lot!.Images.FirstOrDefault(image => image.Id == imageId);
        if (imageToSet is null)
        {
            throw new ArgumentException($"Lot image with ID {imageId} does not belong to lot {lotId}.");
        }

        foreach (var image in lot.Images)
        {
            image.IsMain = false;
        }

        imageToSet.IsMain = true;
        await _repositoryWrapper.SaveChangesAsync();

        return _mapper.Map<LotImageDto>(imageToSet);
    }

    private async Task<Lot?> GetLotWithImagesAsync(int lotId)
    {
        return await _repositoryWrapper.LotsRepository.GetFirstOrDefaultAsync(
            new QueryOptions<Lot>
            {
                Filter = lot => lot.Id == lotId,
                Include = query => query.Include(lot => lot.Images),
                AsNoTracking = false
            });
    }

    private static void EnsureLotOwner(Lot? lot, int userId, string action)
    {
        if (lot is null)
        {
            throw new ArgumentException("Lot not found.");
        }

        if (lot.SellerId != userId)
        {
            throw new UnauthorizedAccessException($"You are not authorized to {action}.");
        }
    }
}
