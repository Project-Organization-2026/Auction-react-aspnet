namespace Auction.BLL.Settings;

public class BlockchainSettings
{
    public bool Enabled { get; set; }
    public long ChainId { get; set; }
    public string RpcUrl { get; set; } = string.Empty;
    public string ContractAddress { get; set; } = string.Empty;
    public int ConfirmationsRequired { get; set; } = 2;

    public static void Validate(BlockchainSettings settings)
    {
        if (!settings.Enabled)
        {
            return;
        }

        if (settings.ChainId <= 0)
        {
            throw new InvalidOperationException("Blockchain chain ID must be positive.");
        }

        if (!Uri.TryCreate(settings.RpcUrl, UriKind.Absolute, out var rpcUri) ||
            rpcUri.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException("Blockchain RPC URL is invalid.");
        }

        if (!IsEthereumAddress(settings.ContractAddress))
        {
            throw new InvalidOperationException("Blockchain contract address is invalid.");
        }

        if (settings.ConfirmationsRequired < 1)
        {
            throw new InvalidOperationException(
                "At least one blockchain confirmation is required.");
        }
    }

    private static bool IsEthereumAddress(string value)
    {
        if (value.Length != 42 ||
            !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var addressBody = value[2..];
        return addressBody.All(Uri.IsHexDigit) &&
               addressBody.Any(character => character != '0');
    }
}
