using Auction.BLL.Services;
using Xunit;

namespace Auction.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void HashAndVerify_WithCorrectPassword_Succeeds()
    {
        var hash = _service.HashPassword("Password123!");

        Assert.NotEqual("Password123!", hash);
        Assert.True(_service.VerifyPassword("Password123!", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_Fails()
    {
        var hash = _service.HashPassword("Password123!");

        Assert.False(_service.VerifyPassword("wrong-password", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-hash")]
    [InlineData("pbkdf2-sha256$invalid$salt$hash")]
    public void Verify_WithMalformedHash_Fails(string hash)
    {
        Assert.False(_service.VerifyPassword("Password123!", hash));
    }
}
