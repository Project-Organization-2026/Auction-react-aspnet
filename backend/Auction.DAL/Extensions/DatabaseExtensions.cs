using Auction.DAL.Data;
using Auction.DAL.Initializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Auction.DAL.Extensions;

public static class DatabaseExtensions
{
    public static async Task<IHost> SeedDatabaseAsync(
        this IHost app,
        Func<string, string> hashPassword)
    {
        using var scope = app.Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(DatabaseExtensions));

        try
        {
            await context.Database.MigrateAsync();

            await SystemDataSeeder.SeedAsync(context);

            var demoSeedEnabled = bool.TryParse(
                configuration["DemoSeed:Enabled"], out var enabled) && enabled;
            if (env.IsDevelopment() || demoSeedEnabled)
            {
                var seedPassword = configuration["DemoSeed:Password"]
                    ?? configuration["DevelopmentSeed:Password"];
                if (seedPassword is not null && seedPassword.Length is < 8 or > 128)
                {
                    throw new InvalidOperationException(
                        "The demo seed password must contain from 8 to 128 characters.");
                }

                await DevelopmentDataSeeder.SeedAsync(
                    context,
                    seedPassword is null ? null : hashPassword(seedPassword));

                if (seedPassword is null)
                {
                    logger.LogInformation(
                        "Demo user and lot seeds were skipped because no seed password is set.");
                }
            }

            await AdminDataSeeder.SeedAsync(context, configuration, hashPassword, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
        return app;
    }
}
