using Auction.BLL.DTOs.LotImages;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Options;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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
        if (string.IsNullOrWhiteSpace(dto.Url))
        {
            throw new ValidationException("Image URL is required.");
        }

        await using var transaction = await _repositoryWrapper.BeginTransactionAsync();
        var lot = await _repositoryWrapper.LotsRepository.GetForUpdateWithImagesAsync(lotId);
        EnsureLotOwner(lot, userId, "add images");

        if (dto.IsMain)
        {
            foreach (var image in lot!.Images)
            {
                image.IsMain = false;
            }

            // Clear the previous main image before inserting the new one because
            // the database has a unique partial index for main images.
            await _repositoryWrapper.SaveChangesAsync();
        }

        var imageToCreate = _mapper.Map<LotImage>(dto);
        imageToCreate.LotId = lotId;

        await _repositoryWrapper.LotImagesRepository.CreateAsync(imageToCreate);
        await _repositoryWrapper.SaveChangesAsync();
        await transaction.CommitAsync();

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
        await using var transaction = await _repositoryWrapper.BeginTransactionAsync();
        var lot = await _repositoryWrapper.LotsRepository.GetForUpdateWithImagesAsync(lotId);
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
        await transaction.CommitAsync();

        return _mapper.Map<LotImageDto>(imageToSet);
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
