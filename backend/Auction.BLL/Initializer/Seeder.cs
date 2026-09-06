using Auction.BLL.DTOs.Lots;
using Auction.BLL.Services;
using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Auction.BLL.Initializer;

public static class Seeder
{
    public static async Task SeedAsync(this IApplicationBuilder builder)
    {
        using var scope = builder.ApplicationServices.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var lotsService = scope.ServiceProvider.GetRequiredService<LotsService>();

        if (context == null)
        {
            return;
        }

        await context.Database.MigrateAsync();

        await SeedCategoriesAsync(context);
        await SeedUsersAsync(context);


        if (lotsService != null )
        {
            await SeedLotsAsync(lotsService, context);
        }

    }

    public static async Task SeedUsersAsync(AuctionDbContext context)
    {
        if (context.Users.Any())
        {
            return;
        }
        var usersToSeed = new List<User>
        {
            new User
            {
                Id = 1,
                UserName = "user1",
                Email = "user1@example.com",
                PasswordHash = "hashedpassword1",
                Role = UserRole.User,
                Balance = 1000.00m,
                CreatedAt = DateTime.UtcNow
            },
        };
        await context.Users.AddRangeAsync(usersToSeed);
        await context.SaveChangesAsync();
    }

    public static async Task SeedCategoriesAsync(AuctionDbContext context)
    {
        if (context.Categories.Any())
        {
            return;
        }
        var categoriesToSeed = new List<Category>
        {
            new Category { Id = 1, Name = "Electronics", Description = "Electronics category" },
        };
        await context.Categories.AddRangeAsync(categoriesToSeed);
        await context.SaveChangesAsync();
    }

    public static async Task SeedLotsAsync(LotsService lotsService, AuctionDbContext context)
    {
        if (context.Lots.Any())
        {
            return;
        }

        var lotsToSeed = new List<CreateLotDto>
        {
            new CreateLotDto
            {
                Title = "Lot 1",
                Description = "Description for Lot 1",
                StartingPrice = 100.00m,
                CategoryId = 1,
                SellerId = 1
            },
            new CreateLotDto
            {
                Title = "Lot 2",
                Description = "Description for Lot 2",
                StartingPrice = 200.00m,
                CategoryId = 1,
                SellerId = 1
            }
        };
        foreach (var lot in lotsToSeed)
        {
            await lotsService.CreateLotAsync(lot);
        }
    }
}
