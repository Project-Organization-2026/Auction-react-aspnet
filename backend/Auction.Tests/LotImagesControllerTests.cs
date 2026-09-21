using Auction.API.Controllers;
using Auction.BLL.DTOs.LotImages;
using Auction.BLL.Services;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Interfaces.LotImages;
using Auction.DAL.Repositories.Interfaces.Lots;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using System.Security.Claims;
using Xunit;

namespace Auction.Tests;

public class LotImagesControllerTests
{
    [Fact]
    public async Task AddImage_WithInvalidUrl_ReturnsBadRequest()
    {
        var wrapper = CreateWrapper();
        var controller = CreateController(wrapper, 10);

        var result = await controller.AddImage(1, new AddLotImageDto { Url = " " });

        Assert.IsType<BadRequestObjectResult>(result);
        wrapper.Verify(item => item.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task AddImage_WithoutUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(CreateWrapper(), null);

        var result = await controller.AddImage(1, new AddLotImageDto { Url = "image" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task DeleteImage_WhenImageDoesNotExist_ReturnsNotFound()
    {
        var wrapper = CreateWrapper();
        wrapper.SetupGet(item => item.LotImagesRepository)
            .Returns(CreateImageRepository(null).Object);
        var controller = CreateController(wrapper, 10);

        var result = await controller.DeleteImage(99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteImage_WithoutUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(CreateWrapper(), null);

        var result = await controller.DeleteImage(1);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task SetMainImage_WhenImageBelongsToAnotherLot_ReturnsNotFound()
    {
        var wrapper = CreateWrapper();
        var lots = new Mock<ILotsRepository>();
        lots.Setup(item => item.GetForUpdateWithImagesAsync(1))
            .ReturnsAsync(new Lot { Id = 1, SellerId = 10, Images = new List<LotImage>() });
        wrapper.SetupGet(item => item.LotsRepository).Returns(lots.Object);
        var controller = CreateController(wrapper, 10);

        var result = await controller.SetMainImage(1, 99);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task SetMainImage_WithoutUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateController(CreateWrapper(), null);

        var result = await controller.SetMainImage(1, 1);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    private static LotImagesController CreateController(
        Mock<IRepositoryWrapper> wrapper,
        int? userId)
    {
        var mapper = new Mock<IMapper>().Object;
        var controller = new LotImagesController(new LotImagesService(wrapper.Object, mapper));
        var identity = userId.HasValue
            ? new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            }, "Test")
            : new ClaimsIdentity();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
        return controller;
    }

    private static Mock<IRepositoryWrapper> CreateWrapper()
    {
        var wrapper = new Mock<IRepositoryWrapper>();
        var transaction = new Mock<IDbContextTransaction>();
        transaction.Setup(item => item.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        wrapper.Setup(item => item.BeginTransactionAsync()).ReturnsAsync(transaction.Object);
        wrapper.Setup(item => item.SaveChangesAsync()).ReturnsAsync(1);
        wrapper.SetupGet(item => item.LotsRepository).Returns(new Mock<ILotsRepository>().Object);
        wrapper.SetupGet(item => item.LotImagesRepository).Returns(CreateImageRepository(null).Object);
        return wrapper;
    }

    private static Mock<ILotImagesRepository> CreateImageRepository(LotImage? image)
    {
        var repository = new Mock<ILotImagesRepository>();
        repository.Setup(item => item.GetFirstOrDefaultAsync(
                It.IsAny<Auction.DAL.Repositories.Options.QueryOptions<LotImage>>()))
            .ReturnsAsync(image);
        return repository;
    }
}
