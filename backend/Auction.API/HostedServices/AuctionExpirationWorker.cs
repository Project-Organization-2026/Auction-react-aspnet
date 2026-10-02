using Auction.BLL.Services;

namespace Auction.API.HostedServices;

public class AuctionExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuctionExpirationWorker> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public AuctionExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<AuctionExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuctionExpirationWorker started.");

        try
        {
            using var periodicTimer = new PeriodicTimer(_checkInterval);

            while (!stoppingToken.IsCancellationRequested &&
                   await periodicTimer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var lotsService = scope.ServiceProvider.GetRequiredService<LotsService>();

                    var closedCount = await lotsService.CloseExpiredLotsAsync();
                    if (closedCount > 0)
                    {
                        _logger.LogInformation(
                            "AuctionExpirationWorker automatically finalized {Count} expired lot(s).",
                            closedCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while executing AuctionExpirationWorker.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful cancellation on app shutdown
        }

        _logger.LogInformation("AuctionExpirationWorker stopping.");
    }
}
