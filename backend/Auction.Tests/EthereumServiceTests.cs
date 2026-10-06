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
        var artifactPath = Path.GetTempFileName();
        await File.WriteAllTextAsync(artifactPath, "{\"bytecode\":\"0x6000\"}");
        var methods = new List<string>();
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
                    "eth_sendTransaction" => "{\"jsonrpc\":\"2.0\",\"result\":\"0xtx\"}",
                    "eth_getTransactionReceipt" => JsonSerializer.Serialize(new
                    {
                        jsonrpc = "2.0",
                        result = new { status = "0x1", contractAddress = ContractAddress }
                    }),
                    "eth_getCode" => "{\"jsonrpc\":\"2.0\",\"result\":\"0x6000\"}",
                    _ => throw new Exception($"Unexpected RPC call: {method}")
                };
            }));
            var service = new EthereumService(client);

            var address = await service.DeployAuctionAsync(2, DateTime.UtcNow.AddMinutes(10), artifactPath);

            Assert.Equal(ContractAddress, address);
            Assert.Equal(["eth_accounts", "eth_sendTransaction", "eth_getTransactionReceipt", "eth_getCode"], methods);
        }
        finally
        {
            File.Delete(artifactPath);
        }
    }

    private sealed class RpcHandler(Func<string, string> responseForMethod) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(body);
            var method = document.RootElement.GetProperty("method").GetString()!;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseForMethod(method), Encoding.UTF8, "application/json")
            };
        }
    }
}
