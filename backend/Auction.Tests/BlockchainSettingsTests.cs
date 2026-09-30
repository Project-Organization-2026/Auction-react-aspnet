using Auction.BLL.Settings;
using Xunit;

namespace Auction.Tests;

public class BlockchainSettingsTests
{
    [Fact]
    public void Validate_WhenDisabled_AllowsEmptyDeploymentConfiguration()
    {
        BlockchainSettings.Validate(new BlockchainSettings());
    }

    [Fact]
    public void Validate_WhenEnabled_AcceptsValidDeploymentConfiguration()
    {
        BlockchainSettings.Validate(CreateValidSettings());
    }

    [Fact]
    public void Validate_WhenEnabled_RejectsInvalidContractAddress()
    {
        var settings = CreateValidSettings();
        settings.ContractAddress = "not-an-address";

        Assert.Throws<InvalidOperationException>(() =>
            BlockchainSettings.Validate(settings));
    }

    [Fact]
    public void Validate_WhenEnabled_RejectsZeroContractAddress()
    {
        var settings = CreateValidSettings();
        settings.ContractAddress = "0x0000000000000000000000000000000000000000";

        Assert.Throws<InvalidOperationException>(() =>
            BlockchainSettings.Validate(settings));
    }

    [Fact]
    public void Validate_WhenEnabled_RejectsInvalidRpcUrl()
    {
        var settings = CreateValidSettings();
        settings.RpcUrl = "file:///unsafe";

        Assert.Throws<InvalidOperationException>(() =>
            BlockchainSettings.Validate(settings));
    }

    private static BlockchainSettings CreateValidSettings()
    {
        return new BlockchainSettings
        {
            Enabled = true,
            ChainId = 31337,
            RpcUrl = "http://127.0.0.1:8545",
            ContractAddress = "0x1111111111111111111111111111111111111111",
            ConfirmationsRequired = 2
        };
    }
}
