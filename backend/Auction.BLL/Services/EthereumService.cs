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

    /// <summary>Deploys a dedicated Auction contract for one active demo lot.</summary>
    public async Task<string> DeployAuctionAsync(int lotId, DateTime endTime, string? artifactPath = null)
    {
        if (lotId <= 0 || endTime <= DateTime.UtcNow)
        {
            throw new InvalidOperationException("Only active, unexpired lots can receive an auction contract.");
        }

        artifactPath ??= Environment.GetEnvironmentVariable("ETHEREUM_CONTRACT_ARTIFACT_PATH")
            ?? Path.Combine(AppContext.BaseDirectory, "contracts", "Auction.json");
        using var artifact = JsonDocument.Parse(await File.ReadAllTextAsync(artifactPath));
        var bytecode = artifact.RootElement.GetProperty("bytecode").GetString();
        if (string.IsNullOrWhiteSpace(bytecode) || !bytecode.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Auction contract bytecode is missing from the build artifact.");
        }

        var startingBidText = Environment.GetEnvironmentVariable("ETHEREUM_STARTING_BID_ETH");
        var startingBidEth = decimal.TryParse(startingBidText, NumberStyles.Number,
            CultureInfo.InvariantCulture, out var configuredBid) && configuredBid > 0
            ? configuredBid
            : 0.01m;
        var startingWei = (BigInteger)(startingBidEth * 1_000_000_000_000_000_000m);
        var durationSeconds = Math.Max(1L, (long)Math.Ceiling((endTime - DateTime.UtcNow).TotalSeconds));
        var deploymentData = bytecode +
            ToUint256Hex(lotId) +
            ToUint256Hex(startingWei) +
            ToUint256Hex(durationSeconds);

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

        var sendResponse = await _httpClient.PostAsJsonAsync(
            _rpcUrl,
            new RpcRequest("eth_sendTransaction", [new { from = deployer, data = deploymentData }], 5));
        sendResponse.EnsureSuccessStatusCode();
        var sent = await sendResponse.Content.ReadFromJsonAsync<RpcResponse<JsonElement>>();
        var txHash = sent?.Result.ValueKind == JsonValueKind.String
            ? sent.Result.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(txHash))
        {
            throw new InvalidOperationException("Ethereum node did not return a contract deployment transaction hash.");
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
                var address = receipt.Result.GetProperty("contractAddress").GetString();
                if (status is not ("0x1" or "0x01") || string.IsNullOrWhiteSpace(address) ||
                    !await HasContractCodeAsync(address))
                {
                    throw new InvalidOperationException("Auction contract deployment failed on Ganache.");
                }

                return address;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new InvalidOperationException("Timed out waiting for the auction contract deployment receipt.");
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
