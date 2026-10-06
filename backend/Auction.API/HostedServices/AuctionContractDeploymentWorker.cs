using Auction.BLL.Services;
using Auction.DAL.Data;
using Auction.DAL.Enums;
using Microsoft.EntityFrameworkCore;

namespace Auction.API.HostedServices;

/// <summary>
/// Gives every active lot its own Auction contract on the configured Ganache node.
/// Existing seeded lots and newly created lots use the same path.
/// </summary>
public sealed class AuctionContractDeploymentWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AuctionContractDeploymentWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!bool.TryParse(Environment.GetEnvironmentVariable("ETHEREUM_AUTO_DEPLOY"), out var enabled) || !enabled)
        {
            logger.LogInformation("Automatic auction contract deployment is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeployMissingContractsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not synchronize auction contracts.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task DeployMissingContractsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<AuctionDbContext>();
        var ethereum = scope.ServiceProvider.GetRequiredService<EthereumService>();
        var now = DateTime.UtcNow;
        var lots = await database.Lots
            .Where(lot => lot.Status == LotStatus.Active && lot.EndTime > now)
            .OrderBy(lot => lot.Id)
            .ToListAsync(cancellationToken);

        foreach (var lot in lots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!string.IsNullOrWhiteSpace(lot.ContractAddress) &&
                    await ethereum.HasContractCodeAsync(lot.ContractAddress))
                {
                    continue;
                }

                var address = await ethereum.DeployAuctionAsync(lot.Id, lot.EndTime);
                lot.ContractAddress = address;
                await database.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Deployed auction contract {ContractAddress} for lot {LotId}.",
                    address, lot.Id);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Could not deploy auction contract for lot {LotId}.", lot.Id);
            }
        }
    }
}
