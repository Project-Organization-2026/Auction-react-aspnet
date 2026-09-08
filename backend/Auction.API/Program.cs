using Auction.BLL.Services;
using Auction.BLL.Settings;
using Auction.DAL.Data;
using Auction.DAL.Extensions;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Realizations;
using Microsoft.EntityFrameworkCore;

// Load local environment variables from .env
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Register repositories and business services
builder.Services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
builder.Services.AddScoped<LotsService>();
builder.Services.AddScoped<JwtService>();

// Register JWT configuration
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add controllers and API services
builder.Services.AddControllers();
builder.Services.AddAutoMapper(
    cfg => { },
    AppDomain.CurrentDomain.GetAssemblies());

// Add Swagger documentation
builder.Services.AddSwaggerGen(cfg => { });

// Configure PostgreSQL database
builder.Services.AddDbContext<AuctionDbContext>(options =>
{
    string? connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection");

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' not found.");
    }

    options.UseNpgsql(connectionString);
});

var app = builder.Build();

// Seed initial development data
await app.SeedDatabaseAsync();

// Enable Swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure HTTP request pipeline
app.UseAuthorization();

app.MapControllers();

app.Run();