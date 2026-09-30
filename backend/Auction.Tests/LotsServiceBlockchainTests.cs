using Auction.BLL.Services;
using Auction.DAL.Entities;
using Auction.DAL.Enums;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Interfaces.Lots;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace Auction.Tests;

public class LotsServiceBlockchainTests
{
    [Fact]
    public async Task CloseLot_ForBlockchainLot_DoesNotUseDatabaseSettlement()
    {
        var lots = new Mock<ILotsRepository>();
        lots.Setup(repository => repository.GetForUpdateAsync(1))
            .ReturnsAsync(new Lot
            {
                Id = 1,
                SellerId = 7,
                Status = LotStatus.Active,
                SettlementMode = AuctionSettlementMode.Blockchain,
                EndTime = DateTime.UtcNow.AddMinutes(-1)
            });

        var transaction = new Mock<IDbContextTransaction>();
        var wrapper = new Mock<IRepositoryWrapper>();
        wrapper.SetupGet(item => item.LotsRepository).Returns(lots.Object);
        wrapper.Setup(item => item.BeginTransactionAsync())
            .ReturnsAsync(transaction.Object);

        var service = new LotsService(wrapper.Object, new Mock<IMapper>().Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CloseLotAsync(1, 7));

        Assert.Contains("smart contract", exception.Message);
        wrapper.Verify(item => item.SaveChangesAsync(), Times.Never);
        transaction.Verify(
            item => item.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
