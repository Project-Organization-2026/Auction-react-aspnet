using System.Net;
using System.Text;
using System.Text.Json;
using Auction.BLL.Services;
using Xunit;

namespace Auction.Tests;

public class EthereumServiceTests
{
    private const string ContractAddress = "0x1545810FD6B1f940C3D955aa2E87BB5744C51002";
    private const string WalletAddress = "0x3688200000000000000000000000000000001ebf";

    [Theory]
    [InlineData("0x0000000000000000000000000000000000000000000000000000000000000000", false)]
    [InlineData("0x0000000000000000000000003688200000000000000000000000000000001ebf", true)]
    public async Task DetectsLiveEthLeaderBeforeFiatFloorReconciliation(string bidder, bool expected)
    {
        using var client = new HttpClient(new RpcHandler(method => method switch
        {
            "eth_call" => JsonSerializer.Serialize(new { jsonrpc = "2.0", result = bidder }),
            _ => throw new Exception($"Unexpected RPC call: {method}")
        }));
        var service = new EthereumService(client);

        Assert.Equal(expected, await service.HasCurrentEthLeaderAsync(ContractAddress));
    }

    [Fact]
    public async Task MissingLeaderResponseDoesNotPermitFiatFloorReconciliation()
    {
        using var client = new HttpClient(new RpcHandler(method => method switch
        {
            "eth_call" => "{\"jsonrpc\":\"2.0\",\"result\":\"0x\"}",
            _ => throw new Exception($"Unexpected RPC call: {method}")
        }));
        var service = new EthereumService(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.HasCurrentEthLeaderAsync(ContractAddress));
    }

    [Fact]
    public async Task VerifyTransactionRejectsLotWithoutContractAddress()
    {
        using var client = new HttpClient(new RpcHandler(_ => throw new Exception("RPC should not be called")));
        var service = new EthereumService(client);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.VerifyTransactionAsync("0xtx", null, 0.1m, WalletAddress));

        Assert.Contains("no auction smart contract", exception.Message);
    }

    [Fact]
    public async Task VerifyTransactionRejectsTransferToAddressWithoutCode()
    {
        using var client = new HttpClient(new RpcHandler(method => method switch
        {
            "eth_getTransactionReceipt" => "{\"jsonrpc\":\"2.0\",\"result\":{\"status\":\"0x1\"}}",
            "eth_getTransactionByHash" => JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                result = new { from = WalletAddress, to = ContractAddress, value = "0x16345785d8a0000" }
            }),
            "eth_getCode" => "{\"jsonrpc\":\"2.0\",\"result\":\"0x\"}",
            _ => throw new Exception($"Unexpected RPC call: {method}")
        }));
        var service = new EthereumService(client);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.VerifyTransactionAsync("0xtx", ContractAddress, 0.1m, WalletAddress));

        Assert.Contains("No auction smart contract is deployed", exception.Message);
    }

    [Fact]
    public async Task DeployAuctionUsesUnlockedGanacheAccountAndChecksDeployedCode()
    {
        var previousFixed = Environment.GetEnvironmentVariable("ETH_USD_RATE_FIXED");
        var previousRate = Environment.GetEnvironmentVariable("ETH_USD_RATE");
        Environment.SetEnvironmentVariable("ETH_USD_RATE_FIXED", "true");
        Environment.SetEnvironmentVariable("ETH_USD_RATE", "3000");
        var artifactPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(artifactPath, "{\"bytecode\":\"0x6000\"}");
        var methods = new List<string>();
        string? suppliedGas = null;
        try
        {
            using var client = new HttpClient(new RpcHandler(method =>
            {
                methods.Add(method);
                return method switch
                {
                    "eth_accounts" => JsonSerializer.Serialize(new
                    {
                        jsonrpc = "2.0",
                        result = new[] { WalletAddress }
                    }),
                    "eth_estimateGas" => "{\"jsonrpc\":\"2.0\",\"result\":\"0xf156a\"}",
                    "eth_sendTransaction" => "{\"jsonrpc\":\"2.0\",\"result\":\"0xtx\"}",
                    "eth_getTransactionReceipt" => JsonSerializer.Serialize(new
                    {
                        jsonrpc = "2.0",
                        result = new { status = "0x1", contractAddress = ContractAddress }
                    }),
                    "eth_getCode" => "{\"jsonrpc\":\"2.0\",\"result\":\"0x6000\"}",
                    _ => throw new Exception($"Unexpected RPC call: {method}")
                };
            }, request =>
            {
                if (request.GetProperty("method").GetString() == "eth_sendTransaction")
                {
                    suppliedGas = request.GetProperty("params")[0].GetProperty("gas").GetString();
                }
            }));
            var service = new EthereumService(client);

            var address = await service.DeployAuctionAsync(2, DateTime.UtcNow.AddMinutes(10), 120m, artifactPath);

            Assert.Equal(ContractAddress, address);
            Assert.Equal(["eth_accounts", "eth_estimateGas", "eth_sendTransaction", "eth_getTransactionReceipt", "eth_getCode"], methods);
            Assert.NotNull(suppliedGas);
            Assert.True(Convert.ToInt64(suppliedGas![2..], 16) > Convert.ToInt64("f156a", 16));
        }
        finally
        {
            File.Delete(artifactPath);
            Environment.SetEnvironmentVariable("ETH_USD_RATE_FIXED", previousFixed);
            Environment.SetEnvironmentVariable("ETH_USD_RATE", previousRate);
        }
    }

    [Fact]
    public async Task DeployAuctionReportsGanacheTransactionError()
    {
        var artifactPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(artifactPath, "{\"bytecode\":\"0x6000\"}");
        try
        {
            using var client = new HttpClient(new RpcHandler(method => method switch
            {
                "eth_accounts" => JsonSerializer.Serialize(new
                {
                    jsonrpc = "2.0",
                    result = new[] { WalletAddress }
                }),
                "eth_estimateGas" => "{\"jsonrpc\":\"2.0\",\"result\":\"0xf156a\"}",
                "eth_sendTransaction" => "{\"jsonrpc\":\"2.0\",\"error\":{\"code\":-32000,\"message\":\"gas limit too low\"}}",
                _ => throw new Exception($"Unexpected RPC call: {method}")
            }));
            var service = new EthereumService(client);

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.DeployAuctionAsync(2, DateTime.UtcNow.AddMinutes(10), 120m, artifactPath));

            Assert.Contains("gas limit too low", error.Message);
        }
        finally
        {
            File.Delete(artifactPath);
        }
    }

    private sealed class RpcHandler(
        Func<string, string> responseForMethod,
        Action<JsonElement>? observeRequest = null) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            observeRequest?.Invoke(document.RootElement);
            var method = document.RootElement.GetProperty("method").GetString()!;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseForMethod(method), Encoding.UTF8, "application/json")
            };
        }
    }
}
