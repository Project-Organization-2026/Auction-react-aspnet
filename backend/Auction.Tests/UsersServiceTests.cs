using Auction.BLL.Services;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Interfaces.Users;
using AutoMapper;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace Auction.Tests;

public class UsersServiceTests
{
    private readonly Mock<IRepositoryWrapper> _wrapper = new();
    private readonly Mock<IUsersRepository> _users = new();
    private readonly Mock<IDbContextTransaction> _transaction = new();
    private readonly UsersService _service;

    public UsersServiceTests()
    {
        _wrapper.SetupGet(item => item.UsersRepository).Returns(_users.Object);
        _wrapper.Setup(item => item.BeginTransactionAsync())
            .ReturnsAsync(_transaction.Object);
        _wrapper.Setup(item => item.SaveChangesAsync()).ReturnsAsync(1);
        _transaction.Setup(item => item.CommitAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _service = new UsersService(_wrapper.Object, new Mock<IMapper>().Object);
    }

    [Fact]
    public async Task TopUpBalance_WithNonPositiveAmount_RejectsBeforeTransaction()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.TopUpBalanceAsync(1, 0));

        _wrapper.Verify(item => item.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task TopUpBalance_WhenResultWouldOverflow_ThrowsValidationError()
    {
        _users.Setup(item => item.GetForUpdateAsync(1))
            .ReturnsAsync(new User { Id = 1, Balance = decimal.MaxValue });

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.TopUpBalanceAsync(1, 1));

        _wrapper.Verify(item => item.SaveChangesAsync(), Times.Never);
        _transaction.Verify(
            item => item.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TopUpBalance_WithValidAmount_SavesAndCommits()
    {
        var user = new User { Id = 1, Balance = 100 };
        _users.Setup(item => item.GetForUpdateAsync(1)).ReturnsAsync(user);

        var balance = await _service.TopUpBalanceAsync(1, 25);

        Assert.Equal(125, balance);
        Assert.Equal(125, user.Balance);
        _wrapper.Verify(item => item.SaveChangesAsync(), Times.Once);
        _transaction.Verify(
            item => item.CommitAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
