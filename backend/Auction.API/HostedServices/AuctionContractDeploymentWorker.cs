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
                    await ethereum.IsUnifiedAuctionAsync(lot.ContractAddress, lot.Id))
                {
                    // The database can still show a USD leader while an ETH transaction
                    // is being registered. Never refund a live on-chain leader.
                    if (lot.CurrentPriceEth is null &&
                        !await ethereum.HasCurrentEthLeaderAsync(lot.ContractAddress))
                    {
                        var requiredFloor = await ethereum.ConvertUsdToWeiAsync(lot.CurrentPrice + lot.MinBidStep);
                        var onChainFloor = await ethereum.GetMinimumBidWeiAsync(lot.ContractAddress);
                        if (onChainFloor < requiredFloor)
                        {
                            await ethereum.RecordFiatBidAsync(
                                lot.ContractAddress, lot.Id, lot.CurrentPrice + lot.MinBidStep);
                        }
                    }
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(lot.ContractAddress) &&
                    await ethereum.HasContractCodeAsync(lot.ContractAddress) &&
                    await database.Bids.AnyAsync(bid =>
                        bid.LotId == lot.Id && bid.Currency == BidCurrency.Eth,
                        cancellationToken))
                {
                    logger.LogWarning(
                        "Lot {LotId} has ETH bids in an older contract. Automatic replacement is paused to preserve those funds.",
                        lot.Id);
                    continue;
                }

                var address = await ethereum.DeployAuctionAsync(lot.Id, lot.EndTime, lot.CurrentPrice + lot.MinBidStep);
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
