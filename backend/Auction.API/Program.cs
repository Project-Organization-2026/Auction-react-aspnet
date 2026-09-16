using Auction.BLL.Services;
using Auction.BLL.Settings;
using Auction.DAL.Data;
using Auction.DAL.Extensions;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Realizations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

// Load local environment variables from .env
DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Register repositories and business services
builder.Services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
builder.Services.AddScoped<LotsService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<BidsService>();

// Register JWT configuration
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrWhiteSpace(jwtSecretKey))
{
    throw new InvalidOperationException("JWT secret key is not configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
