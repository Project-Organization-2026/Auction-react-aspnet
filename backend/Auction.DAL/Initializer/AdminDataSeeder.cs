using Auction.DAL.Data;
using Auction.DAL.Entities;
using Auction.DAL.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Auction.DAL.Initializer;

public static class AdminDataSeeder
{
    public static async Task SeedAsync(
        AuctionDbContext context,
        IConfiguration configuration,
        Func<string, string> hashPassword,
        ILogger logger)
    {
        var email = configuration["ADMIN_EMAIL"]
                    ?? configuration["AdminSeed:Email"]
                    ?? Environment.GetEnvironmentVariable("ADMIN_EMAIL");

        var userName = configuration["ADMIN_USERNAME"]
                       ?? configuration["AdminSeed:UserName"]
                       ?? Environment.GetEnvironmentVariable("ADMIN_USERNAME");

        var password = configuration["ADMIN_PASSWORD"]
                       ?? configuration["AdminSeed:Password"]
                       ?? Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        userName = string.IsNullOrWhiteSpace(userName) ? "admin" : userName.Trim();
        email = email.Trim().ToLowerInvariant();

        if (password.Length is < 8 or > 128)
        {
            throw new InvalidOperationException("ADMIN_PASSWORD must contain from 8 to 128 characters.");
        }

        var existingUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email || u.UserName == userName);

        if (existingUser != null)
        {
            existingUser.Email = email;
            existingUser.UserName = userName;
            existingUser.PasswordHash = hashPassword(password);
            existingUser.Role = UserRole.Admin;
            if (existingUser.Balance < 1000m)
            {
                existingUser.Balance = 1000m;
            }
            logger.LogInformation(
                "Service admin user '{UserName}' ({Email}) successfully configured from .env.",
                userName, email);
        }
        else
        {
            var adminUser = new User
            {
                UserName = userName,
                Email = email,
                PasswordHash = hashPassword(password),
                Role = UserRole.Admin,
                Balance = 1000m,
                CreatedAt = DateTime.UtcNow
            };
            await context.Users.AddAsync(adminUser);
            logger.LogInformation(
                "Service admin user '{UserName}' ({Email}) successfully created from .env.",
                userName, email);
        }

        await context.SaveChangesAsync();
    }
}
