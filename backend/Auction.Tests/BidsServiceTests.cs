using Auction.BLL.DTOs.Bids;
using Auction.BLL.Services;
using Auction.DAL.Entities;
using Auction.DAL.Enums;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Interfaces.Bids;
using Auction.DAL.Repositories.Interfaces.Lots;
using Auction.DAL.Repositories.Interfaces.Users;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace Auction.Tests;

public class BidsServiceTests
{
    private readonly Mock<IRepositoryWrapper> _wrapper = new();
    private readonly Mock<ILotsRepository> _lots = new();
    private readonly Mock<IBidsRepository> _bids = new();
    private readonly Mock<IUsersRepository> _users = new();
    private readonly Mock<IDbContextTransaction> _transaction = new();
    private readonly BidsService _service;

    public BidsServiceTests()
    {
        _wrapper.SetupGet(item => item.LotsRepository).Returns(_lots.Object);
        _wrapper.SetupGet(item => item.BidsRepository).Returns(_bids.Object);
        _wrapper.SetupGet(item => item.UsersRepository).Returns(_users.Object);
        _wrapper.Setup(item => item.BeginTransactionAsync())
            .ReturnsAsync(_transaction.Object);
        _wrapper.Setup(item => item.SaveChangesAsync()).ReturnsAsync(1);
        _transaction.Setup(item => item.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _bids.Setup(item => item.CreateAsync(It.IsAny<Bid>()))
            .ReturnsAsync((Bid bid) => bid);

        var mapper = new Mock<IMapper>();
        mapper.Setup(item => item.Map<Bid>(It.IsAny<CreateBidDto>()))
            .Returns((CreateBidDto dto) => new Bid
            {
                LotId = dto.LotId,
                Amount = dto.Amount
            });
        mapper.Setup(item => item.Map<BidDto>(It.IsAny<Bid>()))
            .Returns((Bid bid) => new BidDto
            {
                Id = bid.Id,
                LotId = bid.LotId,
                UserId = bid.UserId,
                Amount = bid.Amount,
                PlacedAt = bid.PlacedAt
            });

        _service = new BidsService(_wrapper.Object, mapper.Object);
    }

    [Fact]
    public async Task CreateBid_ByLotSeller_IsRejected()
    {
        _lots.Setup(item => item.GetForUpdateAsync(1))
            .ReturnsAsync(CreateActiveLot(sellerId: 5));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateBidAsync(new CreateBidDto { LotId = 1, Amount = 120 }, 5));

        _wrapper.Verify(item => item.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateBid_WithInsufficientBalance_IsRejected()
    {
        _lots.Setup(item => item.GetForUpdateAsync(1))
            .ReturnsAsync(CreateActiveLot(sellerId: 5));
        _bids.Setup(item => item.GetHighestByLotIdAsync(1)).ReturnsAsync((Bid?)null);
        _users.Setup(item => item.GetForUpdateAsync(2))
            .ReturnsAsync(new User { Id = 2, Balance = 50 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateBidAsync(new CreateBidDto { LotId = 1, Amount = 120 }, 2));

        _wrapper.Verify(item => item.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateBid_WithTooManyDecimalPlaces_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.CreateBidAsync(new CreateBidDto { LotId = 1, Amount = 120.005m }, 2));

        _wrapper.Verify(item => item.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateBid_WithValidAmount_DeductsBalanceAndCommits()
    {
        var lot = CreateActiveLot(sellerId: 5);
        var bidder = new User { Id = 2, Balance = 500 };
        _lots.Setup(item => item.GetForUpdateAsync(1)).ReturnsAsync(lot);
        _bids.Setup(item => item.GetHighestByLotIdAsync(1)).ReturnsAsync((Bid?)null);
        _users.Setup(item => item.GetForUpdateAsync(2)).ReturnsAsync(bidder);

        var result = await _service.CreateBidAsync(
            new CreateBidDto { LotId = 1, Amount = 120 },
            2);

        Assert.Equal(120, result.Amount);
        Assert.Equal(380, bidder.Balance);
        Assert.Equal(120, lot.CurrentPrice);
        Assert.Equal(2, lot.WinnerId);
        _wrapper.Verify(item => item.SaveChangesAsync(), Times.Once);
        _transaction.Verify(
            item => item.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static Lot CreateActiveLot(int sellerId)
    {
        return new Lot
        {
            Id = 1,
            SellerId = sellerId,
            Status = LotStatus.Active,
            CurrentPrice = 100,
            MinBidStep = 10,
            EndTime = DateTime.UtcNow.AddHours(1)
        };
    }
}
