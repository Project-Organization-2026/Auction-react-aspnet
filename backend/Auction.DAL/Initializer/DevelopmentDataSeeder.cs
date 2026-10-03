using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace Auction.DAL.Initializer;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(AuctionDbContext context, string? passwordHash)
    {
        await SeedCategoriesAsync(context);
        if (passwordHash is not null)
        {
            await SeedUsersAsync(context, passwordHash);
        }
        await SeedLotsAsync(context);
    }

    public static async Task SeedUsersAsync(AuctionDbContext context, string passwordHash)
    {
        var usersToSeed = new List<User>
        {
            new User
            {
                UserName = "user1",
                Email = "user1@example.com",
                PasswordHash = passwordHash,
                Role = UserRole.User,
                Balance = 1000.00m,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                UserName = "admin",
                Email = "admin@example.com",
                PasswordHash = passwordHash,
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
            new Category { Name = "Electronics", Description = "Electronics, digital gadgets and vintage tech" },
            new Category { Name = "Art & Antiques", Description = "Paintings, sculptures, antique furniture and fine art" },
            new Category { Name = "Collectibles", Description = "Rare collectibles, coins, comics and signed memorabilia" },
            new Category { Name = "Watches & Jewelry", Description = "Luxury timepieces, wristwatches and fine jewelry" },
            new Category { Name = "Fashion & Retro", Description = "Collector vintage clothing, sneakers and accessories" }
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
        var defaultUser = await context.Users.FirstOrDefaultAsync(u => u.UserName == "user1")
                          ?? await context.Users.FirstOrDefaultAsync();
        if (defaultUser == null)
        {
            return;
        }

        var categories = await context.Categories.ToDictionaryAsync(c => c.Name, c => c.Id);
        int GetCat(string name) => categories.TryGetValue(name, out var id) ? id : categories.Values.FirstOrDefault();

        var lotsToSeed = new List<Lot>
        {
            new Lot
            {
                Title = "Vintage 1968 Omega Speedmaster Professional",
                Description = "Legendary 1968 'Moonwatch'. Calibre 861, original stepped dial with tritium patina, stainless steel bracelet. Fully serviced in Switzerland.",
                StartingPrice = 4500.00m,
                CurrentPrice = 4800.00m,
                MinBidStep = 100.00m,
                StartTime = DateTime.UtcNow.AddDays(-2),
                EndTime = DateTime.UtcNow.AddDays(5),
                Status = LotStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                CategoryId = GetCat("Watches & Jewelry"),
                SellerId = defaultUser.Id,
                Images = new List<LotImage>
                {
                    new LotImage { Url = "https://images.unsplash.com/photo-1523275335684-37898b6baf30?w=800&auto=format&fit=crop&q=80", IsMain = true },
                    new LotImage { Url = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?w=800&auto=format&fit=crop&q=80", IsMain = false }
                }
            },
            new Lot
            {
                Title = "Leica M3 Rangefinder Camera (1956)",
                Description = "Classic Double Stroke 35mm rangefinder film camera. Comes with Summicron 50mm f/2 lens. Immaculate collector condition with leather case.",
                StartingPrice = 1200.00m,
                CurrentPrice = 1250.00m,
                MinBidStep = 50.00m,
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddDays(6),
                Status = LotStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CategoryId = GetCat("Electronics"),
                SellerId = defaultUser.Id,
                Images = new List<LotImage>
                {
                    new LotImage { Url = "https://images.unsplash.com/photo-1516035069371-29a1b244cc32?w=800&auto=format&fit=crop&q=80", IsMain = true },
                    new LotImage { Url = "https://images.unsplash.com/photo-1502920917128-1aa500764cbd?w=800&auto=format&fit=crop&q=80", IsMain = false }
                }
            },
            new Lot
            {
                Title = "Original Watercolor: 'Venice Canal at Twilight'",
                Description = "Original watercolor on premium Arches 300gsm cotton cold-press paper. European master artist piece. Framed with museum-grade UV-protective glass.",
                StartingPrice = 350.00m,
                CurrentPrice = 350.00m,
                MinBidStep = 25.00m,
                StartTime = DateTime.UtcNow.AddDays(-3),
                EndTime = DateTime.UtcNow.AddDays(4),
                Status = LotStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                CategoryId = GetCat("Art & Antiques"),
                SellerId = defaultUser.Id,
                Images = new List<LotImage>
                {
                    new LotImage { Url = "https://images.unsplash.com/photo-1579783902614-a3fb3927b675?w=800&auto=format&fit=crop&q=80", IsMain = true }
                }
            },
            new Lot
            {
                Title = "Nike Air Jordan 1 Retro High Chicago (1985)",
                Description = "Authentic 1985 release of the iconic Air Jordan 1 'Chicago' in US Size 10. Holy grail collectors item with original laces and box.",
                StartingPrice = 2800.00m,
                CurrentPrice = 3100.00m,
                MinBidStep = 100.00m,
                StartTime = DateTime.UtcNow.AddDays(-4),
                EndTime = DateTime.UtcNow.AddDays(3),
                Status = LotStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-4),
                CategoryId = GetCat("Fashion & Retro"),
                SellerId = defaultUser.Id,
                Images = new List<LotImage>
                {
                    new LotImage { Url = "https://images.unsplash.com/photo-1552346154-21d32810aba3?w=800&auto=format&fit=crop&q=80", IsMain = true }
                }
            },
            new Lot
            {
                Title = "Apple Macintosh Plus (1986) Fully Working",
                Description = "Vintage Apple Macintosh Plus computer complete with mechanical keyboard, original mouse, and travel bag. 4MB RAM upgrade, verified boots System 6.",
                StartingPrice = 600.00m,
                CurrentPrice = 650.00m,
                MinBidStep = 25.00m,
                StartTime = DateTime.UtcNow.AddDays(-1),
                EndTime = DateTime.UtcNow.AddDays(7),
                Status = LotStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CategoryId = GetCat("Electronics"),
                SellerId = defaultUser.Id,
                Images = new List<LotImage>
                {
                    new LotImage { Url = "https://images.unsplash.com/photo-1550745165-9bc0b252726f?w=800&auto=format&fit=crop&q=80", IsMain = true }
                }
            },
            new Lot
            {
                Title = "First Edition: The Hobbit by J.R.R. Tolkien",
                Description = "Rare collector hardcover edition with dust jacket, decorative maps, and presentation slipcase. Authenticity certificate included.",
                StartingPrice = 1800.00m,
                CurrentPrice = 2200.00m,
                MinBidStep = 100.00m,
                StartTime = DateTime.UtcNow.AddDays(-5),
                EndTime = DateTime.UtcNow.AddDays(2),
                Status = LotStatus.Active,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                CategoryId = GetCat("Collectibles"),
                SellerId = defaultUser.Id,
                Images = new List<LotImage>
                {
                    new LotImage { Url = "https://images.unsplash.com/photo-1544716278-ca5e3f4abd8c?w=800&auto=format&fit=crop&q=80", IsMain = true }
                }
            },
            new Lot
            {
                Title = "Carved Victorian Mahogany Armchair",
                Description = "Late 19th-century solid mahogany armchair with intricate hand carvings and silk damask upholstery. Professionally preserved and restored.",
                StartingPrice = 850.00m,
                CurrentPrice = 850.00m,
                MinBidStep = 50.00m,
                StartTime = DateTime.UtcNow.AddDays(-10),
                EndTime = DateTime.UtcNow.AddDays(-1),
                Status = LotStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                CategoryId = GetCat("Art & Antiques"),
                SellerId = defaultUser.Id,
                Images = new List<LotImage>
                {
                    new LotImage { Url = "https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=800&auto=format&fit=crop&q=80", IsMain = true }
                }
            }
        };

        var existingTitles = await context.Lots.Select(l => l.Title).ToListAsync();
        var missingLots = lotsToSeed.Where(l => !existingTitles.Contains(l.Title)).ToList();

        if (missingLots.Count != 0)
        {
            await context.Lots.AddRangeAsync(missingLots);
            await context.SaveChangesAsync();
        }
    }
}
