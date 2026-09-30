using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Enums;
using Microsoft.EntityFrameworkCore;

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
        var usersToSeed = new List<User>
        {
            new User
            {
                UserName = "user1",
                Email = "user1@example.com",
                // Development-only password: Password123!
                PasswordHash = "pbkdf2-sha256$100000$kqIPGJUj55cZ5hdMpq0ciA==$SeGcoqyDh2T1M3L2TousUQZSRid9FnQzavEKjSvij2M=",
                Role = UserRole.User,
                Balance = 1000.00m,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                UserName = "admin",
                Email = "admin@example.com",
                // Development-only password: Password123!
                PasswordHash = "pbkdf2-sha256$100000$kqIPGJUj55cZ5hdMpq0ciA==$SeGcoqyDh2T1M3L2TousUQZSRid9FnQzavEKjSvij2M=",
                Role = UserRole.Admin,
                Balance = 1000.00m,
                CreatedAt = DateTime.UtcNow
            }
        };
        var existingEmails = await context.Users
            .Select(user => user.Email)
            .ToListAsync();
        var existingUserNames = await context.Users
            .Select(user => user.UserName)
            .ToListAsync();
        var missingUsers = usersToSeed
            .Where(user =>
                !existingEmails.Contains(user.Email) &&
                !existingUserNames.Contains(user.UserName))
            .ToList();

        if (missingUsers.Count != 0)
        {
            await context.Users.AddRangeAsync(missingUsers);
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedCategoriesAsync(AuctionDbContext context)
    {
        var categoriesToSeed = new List<Category>
        {
            new Category { Name = "Electronics", Description = "Electronics category" },
        };
        var existingNames = await context.Categories
            .Select(category => category.Name)
            .ToListAsync();
        var missingCategories = categoriesToSeed
            .Where(category => !existingNames.Contains(category.Name))
            .ToList();

        if (missingCategories.Count != 0)
        {
            await context.Categories.AddRangeAsync(missingCategories);
            await context.SaveChangesAsync();
        }
    }

    public static async Task SeedLotsAsync(AuctionDbContext context)
    {
        if (await context.Lots.AnyAsync())
        {
            return;
        }

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
