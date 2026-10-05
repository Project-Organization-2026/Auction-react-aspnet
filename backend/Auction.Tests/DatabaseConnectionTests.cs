using Auction.API.Configuration;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace Auction.Tests;

public class DatabaseConnectionTests
{
    [Fact]
    public void Resolve_ConvertsPostgresUrlIncludingEncodedCredentials()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_URL"] = "postgresql://auction:pass%3Aword@database.internal:5433/auction_db"
            })
            .Build();

        var connection = new NpgsqlConnectionStringBuilder(DatabaseConnection.Resolve(configuration));

        Assert.Equal("database.internal", connection.Host);
        Assert.Equal(5433, connection.Port);
        Assert.Equal("auction_db", connection.Database);
        Assert.Equal("auction", connection.Username);
        Assert.Equal("pass:word", connection.Password);
    }

    [Fact]
    public void Resolve_PrefersExistingConnectionString()
    {
        const string configured = "Host=localhost;Database=auction;Username=postgres;Password=test";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = configured,
                ["DATABASE_URL"] = "postgresql://other:secret@elsewhere/other"
            })
            .Build();

        Assert.Equal(configured, DatabaseConnection.Resolve(configuration));
    }
}
