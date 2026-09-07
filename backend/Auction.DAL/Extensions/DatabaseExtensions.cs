using Auction.DAL.Data;
using Auction.DAL.Initializer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Auction.DAL.Extensions;

public static class DatabaseExtensions
{
    public static async Task<IHost> SeedDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        try
        {
            await context.Database.MigrateAsync();

            await SystemDataSeeder.SeedAsync(context);

            if (env.IsDevelopment())
            {
                await DevelopmentDataSeeder.SeedAsync(context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
        }
        return app;
    }
}
