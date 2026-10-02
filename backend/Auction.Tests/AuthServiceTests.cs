using Auction.BLL.DTOs.Auth;
using Auction.BLL.DTOs.Users;
using Auction.BLL.Services;
using Auction.BLL.Settings;
using Auction.DAL.Entities;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Interfaces.Users;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Auction.Tests;

public class AuthServiceTests
{
    private readonly Mock<IRepositoryWrapper> _wrapper = new();
    private readonly Mock<IUsersRepository> _users = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _wrapper.SetupGet(item => item.UsersRepository).Returns(_users.Object);
        _wrapper.Setup(item => item.SaveChangesAsync()).ReturnsAsync(1);
        _users.Setup(item => item.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) => user);

        var mapper = new Mock<IMapper>();
        mapper.Setup(item => item.Map<UserSummaryDto>(It.IsAny<User>()))
            .Returns((User user) => new UserSummaryDto
            {
                Id = user.Id,
                UserName = user.UserName
            });

        var jwtService = new JwtService(
            Options.Create(new JwtSettings
            {
                SecretKey = "a-test-secret-key-that-is-at-least-32-bytes",
                Issuer = "tests",
                Audience = "tests",
                ExpireHours = 1
            }),
            NullLogger<JwtService>.Instance);

        _service = new AuthService(
            _wrapper.Object,
            jwtService,
            mapper.Object);
    }

    [Fact]
    public async Task Register_WithAvailableCredentials_HashesPasswordAndReturnsToken()
    {
        User? createdUser = null;
        _users.Setup(item => item.CreateAsync(It.IsAny<User>()))
            .Callback<User>(user => createdUser = user)
            .ReturnsAsync((User user) => user);

        var result = await _service.RegisterAsync(new RegisterUserDto
        {
            UserName = "  test-user  ",
            Email = "  TEST@EXAMPLE.COM ",
            Password = "Password123!"
        });

        Assert.NotNull(createdUser);
        Assert.Equal("test-user", createdUser.UserName);
        Assert.Equal("test@example.com", createdUser.Email);
        Assert.True(PasswordService.VerifyPassword(
            "Password123!",
            createdUser.PasswordHash));
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
    }

    [Fact]
    public async Task Register_WithExistingEmail_IsRejected()
    {
        _users.Setup(item => item.ExistsByEmailAsync("test@example.com"))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.RegisterAsync(new RegisterUserDto
            {
                UserName = "test-user",
                Email = "test@example.com",
                Password = "Password123!"
            }));

        _users.Verify(item => item.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        _users.Setup(item => item.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(new User
            {
                Id = 1,
                UserName = "test-user",
                Email = "test@example.com",
                PasswordHash = PasswordService.HashPassword("Password123!")
            });

        var result = await _service.LoginAsync(new LoginDto
        {
            Email = "TEST@example.com",
            Password = "Password123!"
        });

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.Equal(1, result.User.Id);
    }

    [Fact]
    public async Task Login_WithWrongPassword_IsRejected()
    {
        _users.Setup(item => item.GetByEmailAsync("test@example.com"))
            .ReturnsAsync(new User
            {
                Email = "test@example.com",
                PasswordHash = PasswordService.HashPassword("Password123!")
            });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.LoginAsync(new LoginDto
            {
                Email = "test@example.com",
                Password = "wrong-password"
            }));
    }
}
