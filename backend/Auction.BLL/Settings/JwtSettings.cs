using System.Text;

namespace Auction.BLL.Settings;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpireHours { get; set; } = 1;

    /// <summary>Rejects missing or undersized UTF-8 keys before HS256 is used.</summary>
    public static void ValidateSecretKey(string? secretKey)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("JWT secret key is not configured.");

        if (Encoding.UTF8.GetByteCount(secretKey) < 32)
            throw new InvalidOperationException("JWT secret key must contain at least 32 bytes.");
    }
}
