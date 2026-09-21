using Auction.BLL.DTOs.LotImages;
using Auction.BLL.Services;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Interfaces.LotImages;
using Auction.DAL.Repositories.Interfaces.Lots;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace Auction.Tests;

public class LotImagesServiceTests
{
    private readonly Mock<IRepositoryWrapper> _wrapper = new();
    private readonly Mock<ILotsRepository> _lots = new();
    private readonly Mock<ILotImagesRepository> _images = new();
    private readonly Mock<IDbContextTransaction> _transaction = new();
    private readonly LotImagesService _service;

    public LotImagesServiceTests()
    {
        var mapper = CreateMapper();

        _wrapper.SetupGet(item => item.LotsRepository).Returns(_lots.Object);
        _wrapper.SetupGet(item => item.LotImagesRepository).Returns(_images.Object);
        _wrapper.Setup(item => item.BeginTransactionAsync()).ReturnsAsync(_transaction.Object);
        _wrapper.Setup(item => item.SaveChangesAsync()).ReturnsAsync(1);
        _transaction.Setup(item => item.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new LotImagesService(_wrapper.Object, mapper);
    }

    private static IMapper CreateMapper()
    {
        var mapper = new Mock<IMapper>();
        mapper.Setup(item => item.Map<LotImage>(It.IsAny<AddLotImageDto>()))
            .Returns((AddLotImageDto dto) => new LotImage
            {
                Url = dto.Url,
                IsMain = dto.IsMain
            });
        mapper.Setup(item => item.Map<LotImageDto>(It.IsAny<LotImage>()))
            .Returns((LotImage image) => new LotImageDto
            {
                Id = image.Id,
                Url = image.Url,
                IsMain = image.IsMain,
                LotId = image.LotId,
                CreatedAt = image.CreatedAt
            });
        return mapper.Object;
    }

    [Fact]
    public async Task AddImage_WithEmptyUrl_ThrowsValidationException()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _service.AddImageToLotAsync(1, new AddLotImageDto { Url = " " }, 10));

        Assert.Equal("Image URL is required.", exception.Message);
        _wrapper.Verify(item => item.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task AddMainImage_ClearsPreviousMainImageAndCommits()
    {
        var previous = new LotImage { Id = 2, LotId = 1, IsMain = true, Url = "old" };
        var lot = new Lot { Id = 1, SellerId = 10, Images = new List<LotImage> { previous } };
        _lots.Setup(item => item.GetForUpdateWithImagesAsync(1)).ReturnsAsync(lot);

        var result = await _service.AddImageToLotAsync(
            1,
            new AddLotImageDto { Url = "new", IsMain = true },
            10);

        Assert.False(previous.IsMain);
        Assert.Equal(1, result.LotId);
        Assert.Equal("new", result.Url);
        _transaction.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddImage_ForOtherSeller_ThrowsUnauthorized()
    {
        _lots.Setup(item => item.GetForUpdateWithImagesAsync(1))
            .ReturnsAsync(new Lot { Id = 1, SellerId = 20 });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.AddImageToLotAsync(1, new AddLotImageDto { Url = "image" }, 10));
    }

    [Fact]
    public async Task SetMainImage_ClearsOtherImagesAndCommits()
    {
        var first = new LotImage { Id = 2, LotId = 1, IsMain = true, Url = "first" };
        var second = new LotImage { Id = 3, LotId = 1, Url = "second" };
        _lots.Setup(item => item.GetForUpdateWithImagesAsync(1))
            .ReturnsAsync(new Lot
            {
                Id = 1,
                SellerId = 10,
                Images = new List<LotImage> { first, second }
            });

        var result = await _service.SetMainImageAsync(3, 1, 10);

        Assert.False(first.IsMain);
        Assert.True(second.IsMain);
        Assert.Equal(3, result.Id);
        _transaction.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetMainImage_FromAnotherLot_ThrowsArgumentException()
    {
        _lots.Setup(item => item.GetForUpdateWithImagesAsync(1))
            .ReturnsAsync(new Lot { Id = 1, SellerId = 10, Images = new List<LotImage>() });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SetMainImageAsync(99, 1, 10));
    }

    [Fact]
    public async Task DeleteImage_ByOwnerDeletesAndSaves()
    {
        var image = new LotImage
        {
            Id = 2,
            LotId = 1,
            Url = "image",
            Lot = new Lot { Id = 1, SellerId = 10 }
        };
        _images.Setup(item => item.GetFirstOrDefaultAsync(
                It.IsAny<Auction.DAL.Repositories.Options.QueryOptions<LotImage>>()))
            .ReturnsAsync(image);

        await _service.DeleteImageAsync(2, 10);

        _images.Verify(item => item.Delete(image), Times.Once);
        _wrapper.Verify(item => item.SaveChangesAsync(), Times.Once);
    }
}
