using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Enums;

namespace Auction.DAL.Initializer;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(AuctionDbContext context)
    {
        await SeedCategoriesAsync(context);
        await SeedUsersAsync(context);

        await SeedLotsAsync(context);
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
                Id = 100,
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
            new Category { Id = 100, Name = "Electronics", Description = "Electronics category" },
        };
        await context.Categories.AddRangeAsync(categoriesToSeed);
        await context.SaveChangesAsync();
    }

    public static async Task SeedLotsAsync(AuctionDbContext context)
    {
        if (context.Lots.Any())
        {
            return;
        }

        var lotsToSeed = new List<Lot>
        {
            new Lot
            {
                Id = 100,
                Title = "Lot 1",
                Description = "Description for Lot 1",
                StartingPrice = 100.00m,
                CurrentPrice = 100.00m,
                MinBidStep = 10.00m,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddDays(7),
                Status = LotStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CategoryId = 100,
                SellerId = 100
            },
            new Lot
            {
                Id = 101,
                Title = "Lot 2",
                Description = "Description for Lot 2",
                StartingPrice = 200.00m,
                CurrentPrice = 200.00m,
                MinBidStep = 20.00m,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow.AddDays(7),
                Status = LotStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CategoryId = 100,
                SellerId = 100
            }
        };

        await context.Lots.AddRangeAsync(lotsToSeed);
        await context.SaveChangesAsync();
    }
}
