using Auction.BLL.Services;
using Xunit;

namespace Auction.Tests;

public class PasswordServiceTests
{

    [Fact]
    public void HashAndVerify_WithCorrectPassword_Succeeds()
    {
        var hash = PasswordService.HashPassword("Password123!");

        Assert.NotEqual("Password123!", hash);
        Assert.True(PasswordService.VerifyPassword("Password123!", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_Fails()
    {
        var hash = PasswordService.HashPassword("Password123!");

        Assert.False(PasswordService.VerifyPassword("wrong-password", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-hash")]
    [InlineData("pbkdf2-sha256$invalid$salt$hash")]
    public void Verify_WithMalformedHash_Fails(string hash)
    {
        Assert.False(PasswordService.VerifyPassword("Password123!", hash));
    }
}
