using Auction.API.Filters;
using Auction.BLL.Services;
using Auction.BLL.Settings;
using Auction.DAL.Data;
using Auction.DAL.Extensions;
using Auction.DAL.Repositories.Interfaces;
using Auction.DAL.Repositories.Realizations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
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
builder.Services.AddScoped<LotImagesService>();
builder.Services.AddScoped<CategoriesService>();
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<PasswordService>();

// Register JWT configuration
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<BlockchainSettings>(
    builder.Configuration.GetSection("Blockchain"));

var jwtSettings = builder.Configuration
    .GetSection("JwtSettings")
    .Get<JwtSettings>() ?? new JwtSettings();
JwtSettings.Validate(jwtSettings);
var jwtSecretKey = jwtSettings.SecretKey;

var blockchainSettings = builder.Configuration
    .GetSection("Blockchain")
    .Get<BlockchainSettings>() ?? new BlockchainSettings();
BlockchainSettings.Validate(blockchainSettings);

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
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ServiceResponse.Error("Authentication is required."));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(
                    ServiceResponse.Error("Access is forbidden."));
            }
        };
    });

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add controllers and API services
builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ServiceResponseResultFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value?.Errors.Count > 0)
                .ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value!.Errors
                        .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "The supplied value is invalid."
                            : error.ErrorMessage)
                        .ToArray());

            return new BadRequestObjectResult(
                ServiceResponse.Error(
                    "Request validation failed.",
                    new { errors }));
        };
    });
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
            .AllowAnyMethod();
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
app.UseMiddleware<Auction.API.Middleware.ExceptionHandlingMiddleware>();
app.UseStatusCodePages(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    response.ContentType = "application/json";
    await response.WriteAsJsonAsync(
        ServiceResponse.Error(
            ServiceResponseResultFilter.GetDefaultErrorMessage(
                response.StatusCode)));
});

// Seed initial development data
await app.SeedDatabaseAsync();

// Enable Swagger only in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure HTTP request pipeline
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(
    ServiceResponse.Success("API is healthy.", new
    {
        status = "Healthy",
        timestamp = DateTime.UtcNow
    }))).AllowAnonymous();

app.Run();
