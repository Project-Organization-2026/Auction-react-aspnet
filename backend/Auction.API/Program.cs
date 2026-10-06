using Auction.API.HostedServices;
using Auction.API.Hubs;
using Auction.API.Configuration;
using Auction.BLL.Services;
using Auction.BLL.Settings;
using Auction.DAL.Data;
using Auction.DAL.Extensions;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Realizations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Globalization;
using System.Security.Claims;
using System.Text;

// Ensure culture-invariant parsing and formatting across all threads
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// Load local environment variables from root or parent .env
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Register repositories and business services
builder.Services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
builder.Services.AddScoped<LotsService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<BidsService>();
builder.Services.AddScoped<LotImagesService>();
builder.Services.AddScoped<CategoriesService>();
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHostedService<AuctionExpirationWorker>();
builder.Services.AddHostedService<AuctionContractDeploymentWorker>();
builder.Services.AddSignalR();

// Ethereum integration: named HttpClient used by EthereumService
// (one client hits CoinGecko; RPC node URL is read from env inside the service)
builder.Services.AddHttpClient<EthereumService>(client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("User-Agent", "BestAuction/1.0");
});

// Register JWT configuration
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>() ?? new JwtSettings();
JwtSettings.Validate(jwtSettings);
var jwtSecretKey = jwtSettings.SecretKey;

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
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = "role"
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

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Add Swagger documentation
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter the JWT access token."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = []
    });
});

// Configure PostgreSQL database
builder.Services.AddDbContext<AuctionDbContext>(options =>
{
    options.UseNpgsql(DatabaseConnection.Resolve(builder.Configuration));
});

var app = builder.Build();
app.UseMiddleware<Auction.API.Middleware.ExceptionHandlingMiddleware>();
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    response.ContentType = "application/json";
    await response.WriteAsJsonAsync(new
    {
        message = response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => "Authentication is required.",
            StatusCodes.Status403Forbidden => "Access is forbidden.",
            StatusCodes.Status404NotFound => "The requested resource was not found.",
            _ => "Request failed."
        }
    });
});

// Seed initial development data
await app.SeedDatabaseAsync(PasswordService.HashPassword);

// Enable Swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure HTTP request pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<AuctionHub>("/hubs/auction");
app.MapGet("/api/health", () => Results.Ok(
    new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow
    })).AllowAnonymous();

// The container serves the built React app from wwwroot. Keep API and hub
// misses as real 404s while letting React handle its own client-side routes.
var frontendIndex = Path.Combine(app.Environment.WebRootPath ?? "", "index.html");
if (File.Exists(frontendIndex))
{
    app.MapFallback((HttpContext context) =>
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/api") ||
            path.StartsWithSegments("/hubs") ||
            path.StartsWithSegments("/uploads"))
        {
            return Results.NotFound();
        }

        return Results.File(frontendIndex, "text/html");
    });
}

app.Run();
