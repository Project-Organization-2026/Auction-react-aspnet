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

    // CoinGecko free-tier rate cache
    private decimal _cachedEthToUsdRate = 3000m;
    private DateTime _cacheExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _rateLock = new(1, 1);
    private const int RateCacheTtlSeconds = 60;

    public EthereumService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _rpcUrl = Environment.GetEnvironmentVariable("ETHEREUM_RPC_URL") ?? "http://127.0.0.1:7545";

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
        var rate = await GetEthToUsdRateAsync();
        return Math.Round(amountEth * rate, 2);
    }

    public async Task<decimal> ConvertUsdToEthAsync(decimal amountUsd)
    {
        var rate = await GetEthToUsdRateAsync();
        return rate > 0 ? Math.Round(amountUsd / rate, 8) : 0;
    }

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

        if (receiptResult.Result.TryGetProperty("status", out var statusProp))
        {
            var statusStr = statusProp.GetString();
            if (statusStr != "0x1" && statusStr != "0x01")
            {
                throw new InvalidOperationException("Transaction failed or was reverted on the blockchain.");
            }
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

        // Verify recipient contract (if contract address is stored on lot)
        if (!string.IsNullOrEmpty(expectedContractAddress) &&
            !string.Equals(to, expectedContractAddress, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Transaction recipient does not match the auction smart contract ({expectedContractAddress}).");
        }

        // Verify value (allow ±0.0001 ETH rounding tolerance)
        if (!string.IsNullOrEmpty(valueHex))
        {
            var rawHex = valueHex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? valueHex[2..]
                : valueHex;

            if (BigInteger.TryParse(rawHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var valueInWei))
            {
                var expectedWei = (BigInteger)(expectedAmountEth * 1_000_000_000_000_000_000m);
                var tolerance = (BigInteger)(0.0001m * 1_000_000_000_000_000_000m);

                if (valueInWei + tolerance < expectedWei)
                {
                    throw new InvalidOperationException(
                        "Transaction value is less than the claimed bid amount.");
                }
            }
        }
    }

    // ─── Private models ───────────────────────────────────────────────────────

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
