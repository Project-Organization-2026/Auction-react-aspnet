using Auction.DAL.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Auction.Tests;

public class MigrationDiscoveryTests
{
    [Fact]
    public void BlockchainAndMainImageMigrations_AreDiscoverable()
    {
        var options = new DbContextOptionsBuilder<AuctionDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;
        using var context = new AuctionDbContext(options);

        var migrations = context.Database.GetMigrations().ToArray();

        Assert.Contains("20260921120000_AddUniqueMainLotImageIndex", migrations);
        Assert.Contains("20260930120000_PrepareBlockchainIntegration", migrations);
        Assert.False(context.Database.HasPendingModelChanges());
    }
}
