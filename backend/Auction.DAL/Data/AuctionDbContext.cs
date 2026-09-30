using Microsoft.EntityFrameworkCore;
using Auction.DAL.Entities;

namespace Auction.DAL.Data;

public class AuctionDbContext : DbContext
{
    public AuctionDbContext(DbContextOptions<AuctionDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<Lot> Lots { get; set; } = null!;
    public DbSet<LotImage> LotImages { get; set; } = null!;
    public DbSet<Bid> Bids { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.WalletAddress).HasMaxLength(42);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.HasIndex(e => e.WalletAddress)
                .IsUnique()
                .HasFilter("\"WalletAddress\" IS NOT NULL");
        });

        // Category configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(1000);
        });

        // Lot configuration
        modelBuilder.Entity<Lot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(512);
            entity.Property(e => e.Description).HasMaxLength(5000);
            entity.Property(e => e.StartingPrice).HasPrecision(18, 2);
            entity.Property(e => e.CurrentPrice).HasPrecision(18, 2);
            entity.Property(e => e.MinBidStep).HasPrecision(18, 2);
            entity.Property(e => e.ContractAddress).HasMaxLength(42);
            entity.Property(e => e.OnChainAuctionId).HasMaxLength(78);
            entity.Property(e => e.CreationTransactionHash).HasMaxLength(66);
            entity.Property(e => e.SettlementTransactionHash).HasMaxLength(66);
            entity.HasIndex(e => new { e.ContractAddress, e.OnChainAuctionId })
                .IsUnique()
                .HasFilter(
                    "\"ContractAddress\" IS NOT NULL AND \"OnChainAuctionId\" IS NOT NULL");

            // Foreign key - Seller
            entity.HasOne(e => e.Seller)
                .WithMany(u => u.CreatedLots)
                .HasForeignKey(e => e.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key - Winner
            entity.HasOne(e => e.Winner)
                .WithMany(u => u.WonLots)
                .HasForeignKey(e => e.WinnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Foreign key - Category
            entity.HasOne(e => e.Category)
                .WithMany(c => c.Lots)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // LotImage configuration
        modelBuilder.Entity<LotImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Url).IsRequired();

            // Keep the regular LotId index for loading all images, and add a separate
            // filtered unique index as a database safeguard for the main image.
            entity.HasIndex(e => e.LotId);
            entity.HasIndex(e => e.LotId)
                .HasDatabaseName("IX_LotImages_LotId_IsMain")
                .IsUnique()
                .HasFilter("\"IsMain\" = TRUE");

            entity.HasOne(e => e.Lot)
                .WithMany(l => l.Images)
                .HasForeignKey(e => e.LotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Bid configuration
        modelBuilder.Entity<Bid>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.AmountWei).HasMaxLength(78);
            entity.Property(e => e.TransactionHash).HasMaxLength(66);
            entity.HasIndex(e => e.TransactionHash)
                .IsUnique()
                .HasFilter("\"TransactionHash\" IS NOT NULL");

            // Foreign key - Lot
            entity.HasOne(e => e.Lot)
                .WithMany(l => l.Bids)
                .HasForeignKey(e => e.LotId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key - User
            entity.HasOne(e => e.User)
                .WithMany(u => u.Bids)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for quick lookups
            entity.HasIndex(e => new { e.LotId, e.PlacedAt });
        });
    }
}
