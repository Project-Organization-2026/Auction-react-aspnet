using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace Auction.DAL.Initializer;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(AuctionDbContext context)
    {
        if (await CheckIfDataExists(context))
        {
            return;
        }

        await SeedCategoriesAsync(context);
        await SeedUsersAsync(context);

        await SeedLotsAsync(context);
    }

    public static async Task<bool> CheckIfDataExists(AuctionDbContext context)
        => await context.Users.AnyAsync() || await context.Categories.AnyAsync() || await context.Lots.AnyAsync();

    public static async Task SeedUsersAsync(AuctionDbContext context)
    {
        var usersToSeed = new List<User>
        {
            new User
            {
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
        var categoriesToSeed = new List<Category>
        {
            new Category { Name = "Electronics", Description = "Electronics category" },
        };
        await context.Categories.AddRangeAsync(categoriesToSeed);
        await context.SaveChangesAsync();
    }

    public static async Task SeedLotsAsync(AuctionDbContext context)
    {
        var defaultCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Electronics");
        var defaultUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "user1");

        if (defaultUser == null)
        {
            throw new InvalidOperationException("No default user found to associate with the lots.");
        }

        if (defaultCategory == null)
        {
            throw new InvalidOperationException("No default category found to associate with the lots.");
        }

        var lotsToSeed = new List<Lot>
        {
            new Lot
            {
                Title = "Lot 1",
                Description = "Description for Lot 1",
                StartingPrice = 100.00m,
                CurrentPrice = 100.00m,
                MinBidStep = 10.00m,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddDays(7),
                Status = LotStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CategoryId = defaultCategory.Id,
                SellerId = defaultUser.Id
            },
            new Lot
            {
                Title = "Lot 2",
                Description = "Description for Lot 2",
                StartingPrice = 200.00m,
                CurrentPrice = 200.00m,
                MinBidStep = 20.00m,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddDays(7),
                Status = LotStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CategoryId = defaultCategory.Id,
                SellerId = defaultUser.Id
            }
        };

        await context.Lots.AddRangeAsync(lotsToSeed);
        await context.SaveChangesAsync();
    }
}
