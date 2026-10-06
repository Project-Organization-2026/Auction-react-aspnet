using System.Globalization;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Auction.BLL.Services;

public class EthereumService
{
    private readonly HttpClient _httpClient;
    private readonly string _rpcUrl;
    private readonly bool _fixedRateEnabled;

    // CoinGecko free-tier rate cache
    private decimal _cachedEthToUsdRate = 3000m;
    private DateTime _cacheExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _rateLock = new(1, 1);
    private const int RateCacheTtlSeconds = 60;

    public EthereumService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _rpcUrl = Environment.GetEnvironmentVariable("ETHEREUM_RPC_URL") ?? "http://127.0.0.1:7545";
        _fixedRateEnabled = !bool.TryParse(
            Environment.GetEnvironmentVariable("ETH_USD_RATE_FIXED"), out var fixedRate) || fixedRate;

        // Seed cache from env fallback so first conversion is never 0
        if (decimal.TryParse(
                Environment.GetEnvironmentVariable("ETH_USD_RATE"),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var envRate) && envRate > 0)
        {
            _cachedEthToUsdRate = envRate;
        }
    }

    /// <summary>
    /// Returns the live ETH/USD rate from CoinGecko (cached for 60 seconds).
    /// Falls back to the last cached value if the remote call fails.
    /// </summary>
    public async Task<decimal> GetEthToUsdRateAsync()
    {
        if (_fixedRateEnabled)
        {
            return _cachedEthToUsdRate;
        }

        if (DateTime.UtcNow < _cacheExpiresAt)
        {
            return _cachedEthToUsdRate;
        }

        await _rateLock.WaitAsync();
        try
        {
            // Double-checked locking inside semaphore
            if (DateTime.UtcNow < _cacheExpiresAt)
            {
                return _cachedEthToUsdRate;
            }

            var url = "https://api.coingecko.com/api/v3/simple/price?ids=ethereum&vs_currencies=usd";
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var result = await _httpClient.GetFromJsonAsync<CoinGeckoSimplePriceResponse>(url, cts.Token);

            if (result?.Ethereum?.Usd is > 0)
            {
                _cachedEthToUsdRate = result.Ethereum.Usd;
                _cacheExpiresAt = DateTime.UtcNow.AddSeconds(RateCacheTtlSeconds);
            }
        }
        catch
        {
            // Network error: silently fall back to last cached value
        }
        finally
        {
            _rateLock.Release();
        }

        return _cachedEthToUsdRate;
    }

    public async Task<decimal> ConvertEthToUsdAsync(decimal amountEth)
    {
        RequireFixedRate();
        var rate = await GetEthToUsdRateAsync();
        return Math.Round(amountEth * rate, 2);
    }

    public async Task<decimal> ConvertUsdToEthAsync(decimal amountUsd)
    {
        var rate = await GetEthToUsdRateAsync();
        return rate > 0 ? Math.Round(amountUsd / rate, 8) : 0;
    }

    public async Task<bool> HasContractCodeAsync(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return false;

        var response = await _httpClient.PostAsJsonAsync(
            _rpcUrl,
            new RpcRequest("eth_getCode", [address, "latest"], 3));
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to check the auction smart contract on Ethereum node.");
        }

        var result = await response.Content.ReadFromJsonAsync<RpcResponse<JsonElement>>();
        if (result?.Error is { ValueKind: JsonValueKind.Object })
        {
            throw new InvalidOperationException("Ethereum node rejected the contract code check.");
        }

        return result?.Result.ValueKind == JsonValueKind.String &&
               !string.IsNullOrWhiteSpace(result.Result.GetString()) &&
               result.Result.GetString() != "0x";
    }

    public async Task<bool> IsUnifiedAuctionAsync(string address, int lotId)
    {
        if (!await HasContractCodeAsync(address)) return false;
        var minimumBid = await ReadContractUint256Async(address, "0xd3a86386");
        var onChainLotId = await ReadContractUint256Async(address, "0xadc1e206");
        return minimumBid.HasValue && onChainLotId == lotId;
    }

    public Task<BigInteger?> GetMinimumBidWeiAsync(string address) =>
        ReadContractUint256Async(address, "0xd3a86386");

    public async Task<bool> IsCurrentEthLeaderAsync(string address, string wallet, decimal amountEth)
    {
        var bidder = await ReadContractUint256Async(address, "0x91f90157");
        var amount = await ReadContractUint256Async(address, "0xd57bde79");
        var walletNumber = BigInteger.Parse("0" + wallet[2..], NumberStyles.HexNumber,
            CultureInfo.InvariantCulture);
        var expectedWei = (BigInteger)(amountEth * 1_000_000_000_000_000_000m);
        return bidder == walletNumber && amount == expectedWei;
    }

    private async Task<BigInteger?> ReadContractUint256Async(string address, string data)
    {
        var response = await _httpClient.PostAsJsonAsync(
            _rpcUrl,
            new RpcRequest("eth_call", [new { to = address, data }, "latest"], 7));
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RpcResponse<JsonElement>>();
        if (result?.Error is { ValueKind: JsonValueKind.Object })
        {
            // Older contracts do not implement the unified-auction getter.
            return null;
        }

        var value = result?.Result.ValueKind == JsonValueKind.String
            ? result.Result.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(value) || value == "0x") return null;
        return BigInteger.TryParse("0" + value[2..], NumberStyles.HexNumber,
            CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }

    /// <summary>Deploys a dedicated Auction contract for one active demo lot.</summary>
    public async Task<string> DeployAuctionAsync(int lotId, DateTime endTime, decimal startingPriceUsd, string? artifactPath = null)
    {
        if (lotId <= 0 || endTime <= DateTime.UtcNow || startingPriceUsd <= 0)
        {
            throw new InvalidOperationException("Only active, unexpired lots can receive an auction contract.");
        }

        RequireFixedRate();

        artifactPath ??= Environment.GetEnvironmentVariable("ETHEREUM_CONTRACT_ARTIFACT_PATH")
            ?? Path.Combine(AppContext.BaseDirectory, "contracts", "Auction.json");
        using var artifact = JsonDocument.Parse(await File.ReadAllTextAsync(artifactPath));
        var bytecode = artifact.RootElement.GetProperty("bytecode").GetString();
        if (string.IsNullOrWhiteSpace(bytecode) || !bytecode.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Auction contract bytecode is missing from the build artifact.");
        }

        var startingWei = await ConvertUsdToWeiAsync(startingPriceUsd);
        var durationSeconds = Math.Max(1L, (long)Math.Ceiling((endTime - DateTime.UtcNow).TotalSeconds));
        var deploymentData = bytecode +
            ToUint256Hex(lotId) +
            ToUint256Hex(startingWei) +
            ToUint256Hex(durationSeconds);

        var receipt = await SendOperatorTransactionAsync(null, deploymentData);
        var address = receipt.TryGetProperty("contractAddress", out var addressValue)
            ? addressValue.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(address) || !await HasContractCodeAsync(address))
        {
            throw new InvalidOperationException("Auction contract deployment failed on Ganache.");
        }

        return address;
    }

    public async Task RecordFiatBidAsync(string contractAddress, int lotId, decimal newPriceUsd)
    {
        RequireFixedRate();
        if (!await IsUnifiedAuctionAsync(contractAddress, lotId))
        {
            throw new InvalidOperationException("This lot needs a compatible auction contract before USD bidding can continue.");
        }
        var minimumWei = await ConvertUsdToWeiAsync(newPriceUsd);
        await SendOperatorTransactionAsync(
            contractAddress,
            "0x248a353f" + ToUint256Hex(minimumWei));
    }

    private async Task<JsonElement> SendOperatorTransactionAsync(string? to, string data)
    {
        var accountsResponse = await _httpClient.PostAsJsonAsync(
            _rpcUrl,
            new RpcRequest("eth_accounts", [], 4));
        accountsResponse.EnsureSuccessStatusCode();
        var accounts = await accountsResponse.Content.ReadFromJsonAsync<RpcResponse<JsonElement>>();
        if (accounts?.Result.ValueKind != JsonValueKind.Array || accounts.Result.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Ganache has no unlocked deployer accounts.");
        }

        var deployer = Environment.GetEnvironmentVariable("ETHEREUM_DEPLOYER_ADDRESS")
            ?? accounts.Result[0].GetString();
        if (string.IsNullOrWhiteSpace(deployer) ||
            !accounts.Result.EnumerateArray().Any(account =>
                string.Equals(account.GetString(), deployer, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("Configured contract deployer is not unlocked on Ganache.");
        }

        object estimateTransaction = to is null
            ? new { from = deployer, data }
            : new { from = deployer, to, data };
        var gasLimit = await EstimateGasAsync(estimateTransaction);
        var gas = "0x" + gasLimit.ToString("x", CultureInfo.InvariantCulture).TrimStart('0');
        object transaction = to is null
            ? new { from = deployer, data, gas }
            : new { from = deployer, to, data, gas };
        var sendResponse = await _httpClient.PostAsJsonAsync(
            _rpcUrl,
            new RpcRequest("eth_sendTransaction", [transaction], 5));
        sendResponse.EnsureSuccessStatusCode();
        var sent = await sendResponse.Content.ReadFromJsonAsync<RpcResponse<JsonElement>>();
        if (sent?.Error is { ValueKind: JsonValueKind.Object } error)
        {
            throw new InvalidOperationException(
                $"Ethereum node rejected the auction transaction: {GetRpcErrorMessage(error)}");
        }
        var txHash = sent?.Result.ValueKind == JsonValueKind.String
            ? sent.Result.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(txHash))
        {
            throw new InvalidOperationException("Ethereum node did not return a transaction hash.");
        }

        for (var attempt = 0; attempt < 30; attempt++)
        {
            var receiptResponse = await _httpClient.PostAsJsonAsync(
                _rpcUrl,
                new RpcRequest("eth_getTransactionReceipt", [txHash], 6));
            receiptResponse.EnsureSuccessStatusCode();
            var receipt = await receiptResponse.Content.ReadFromJsonAsync<RpcResponse<JsonElement>>();
            if (receipt?.Result.ValueKind == JsonValueKind.Object)
            {
                var status = receipt.Result.GetProperty("status").GetString();
                if (status is not ("0x1" or "0x01"))
                {
                    throw new InvalidOperationException("Auction contract transaction reverted on Ganache.");
                }

                return receipt.Result;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new InvalidOperationException("Timed out waiting for the auction contract transaction receipt.");
    }

    private async Task<BigInteger> EstimateGasAsync(object transaction)
    {
        var response = await _httpClient.PostAsJsonAsync(
            _rpcUrl, new RpcRequest("eth_estimateGas", [transaction], 8));
        response.EnsureSuccessStatusCode();
        var estimate = await response.Content.ReadFromJsonAsync<RpcResponse<JsonElement>>();
        if (estimate?.Error is { ValueKind: JsonValueKind.Object } error)
        {
            throw new InvalidOperationException(
                $"Ethereum node could not estimate auction gas: {GetRpcErrorMessage(error)}");
        }

        var gasHex = estimate?.Result.ValueKind == JsonValueKind.String
            ? estimate.Result.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(gasHex) || !gasHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            !BigInteger.TryParse("0" + gasHex[2..], NumberStyles.HexNumber,
                CultureInfo.InvariantCulture, out var estimatedGas) || estimatedGas <= 0)
        {
            throw new InvalidOperationException("Ethereum node returned an invalid gas estimate.");
        }

        return estimatedGas + estimatedGas / 5 + 10_000;
    }

    private static string GetRpcErrorMessage(JsonElement error) =>
        error.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String
            ? message.GetString() ?? "Unknown JSON-RPC error."
            : "Unknown JSON-RPC error.";

    /// <summary>
    /// Verifies that the on-chain transaction is successful, sent to the correct contract,
    /// by the claimed wallet, and carries at least the claimed ETH value.
    /// Throws <see cref="InvalidOperationException"/> on any mismatch.
    /// </summary>
    public async Task VerifyTransactionAsync(
        string txHash,
        string? expectedContractAddress,
        decimal expectedAmountEth,
        string expectedSenderWallet)
    {
        if (string.IsNullOrWhiteSpace(expectedContractAddress))
        {
            throw new InvalidOperationException("This lot has no auction smart contract configured.");
        }

        // 1. Receipt — confirm status == 0x1 (not reverted)
        var receiptPayload = new RpcRequest("eth_getTransactionReceipt", [txHash], 1);
        var receiptResponse = await _httpClient.PostAsJsonAsync(_rpcUrl, receiptPayload);
        if (!receiptResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to communicate with Ethereum node.");
        }

        var receiptResult = await receiptResponse.Content.ReadFromJsonAsync<RpcResponse<JsonElement>>();
        if (receiptResult?.Result.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Transaction receipt not found or still pending.");
        }

        if (!receiptResult.Result.TryGetProperty("status", out var statusProp) ||
            (statusProp.GetString() != "0x1" && statusProp.GetString() != "0x01"))
        {
            throw new InvalidOperationException("Transaction failed or was reverted on the blockchain.");
        }

        // 2. Transaction details — confirm from, to, value
        var txPayload = new RpcRequest("eth_getTransactionByHash", [txHash], 2);
        var txResponse = await _httpClient.PostAsJsonAsync(_rpcUrl, txPayload);
        if (!txResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Failed to fetch transaction details from Ethereum node.");
        }

        var txResult = await txResponse.Content.ReadFromJsonAsync<RpcResponse<JsonElement>>();
        if (txResult?.Result.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Transaction details not found on blockchain.");
        }

        var root = txResult.Result;
        var from = root.GetProperty("from").GetString();
        var to = root.TryGetProperty("to", out var toProp) ? toProp.GetString() : null;
        var valueHex = root.GetProperty("value").GetString();

        // Verify sender
        if (string.IsNullOrEmpty(from) ||
            !string.Equals(from, expectedSenderWallet, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Transaction sender ({from}) does not match bidder wallet ({expectedSenderWallet}).");
        }

        // A confirmed ETH transfer to an empty address is not an auction bid.
        if (!string.Equals(to, expectedContractAddress, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Transaction recipient does not match the auction smart contract ({expectedContractAddress}).");
        }

        if (!await HasContractCodeAsync(expectedContractAddress))
        {
            throw new InvalidOperationException("No auction smart contract is deployed at the configured address.");
        }

        // The registered bid must equal the value actually paid into the contract.
        if (string.IsNullOrEmpty(valueHex))
        {
            throw new InvalidOperationException("Transaction value is missing.");
        }

        var rawHex = valueHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? valueHex[2..]
            : valueHex;

        // Prefix a zero so BigInteger parses hex as an unsigned positive value.
        if (!BigInteger.TryParse("0" + rawHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var valueInWei))
        {
            throw new InvalidOperationException("Transaction value is invalid.");
        }

        var expectedWei = (BigInteger)(expectedAmountEth * 1_000_000_000_000_000_000m);
        if (valueInWei != expectedWei)
        {
            throw new InvalidOperationException("Transaction value does not match the claimed bid amount.");
        }
    }

    // ─── Private models ───────────────────────────────────────────────────────

    private static string ToUint256Hex(BigInteger value)
    {
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        return value.ToString("x", CultureInfo.InvariantCulture).PadLeft(64, '0');
    }

    private void RequireFixedRate()
    {
        if (!_fixedRateEnabled)
        {
            throw new InvalidOperationException(
                "Combined USD/ETH auctions require ETH_USD_RATE_FIXED=true and a fixed ETH_USD_RATE.");
        }
    }

    public async Task<BigInteger> ConvertUsdToWeiAsync(decimal amountUsd)
    {
        RequireFixedRate();
        var rate = await GetEthToUsdRateAsync();
        if (rate <= 0 || amountUsd <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amountUsd));
        }

        var eth = amountUsd / rate;
        var wholeEth = new BigInteger(decimal.Truncate(eth));
        var fractionalWei = (BigInteger)decimal.Ceiling(
            (eth - decimal.Truncate(eth)) * 1_000_000_000_000_000_000m);
        return wholeEth * BigInteger.Pow(10, 18) + fractionalWei;
    }

    private sealed record RpcRequest(
        [property: JsonPropertyName("method")] string Method,
        [property: JsonPropertyName("params")] object[] Params,
        [property: JsonPropertyName("id")]     int Id)
    {
        [JsonPropertyName("jsonrpc")]
        public string JsonRpc { get; } = "2.0";
    }

    private sealed class RpcResponse<T>
    {
        [JsonPropertyName("result")]
        public T Result { get; set; } = default!;

        [JsonPropertyName("error")]
        public JsonElement? Error { get; set; }
    }

    private sealed class CoinGeckoSimplePriceResponse
    {
        [JsonPropertyName("ethereum")]
        public CoinGeckoEthPrice? Ethereum { get; set; }
    }

    private sealed class CoinGeckoEthPrice
    {
        [JsonPropertyName("usd")]
        public decimal Usd { get; set; }
    }
}
