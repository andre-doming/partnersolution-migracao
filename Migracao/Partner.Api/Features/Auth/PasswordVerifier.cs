using System.Security.Cryptography;
using System.Text;

namespace Partner.Api.Features.Auth;

public static class PasswordVerifier
{
    public static bool VerifyLegacyMd5(string rawPassword, string hash)
    {
        if (string.IsNullOrWhiteSpace(rawPassword) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        using var md5 = MD5.Create();
        var computedHash = md5.ComputeHash(Encoding.UTF8.GetBytes(rawPassword));
        var computedText = Convert.ToHexString(computedHash).ToLowerInvariant();
        return string.Equals(computedText, hash, StringComparison.OrdinalIgnoreCase);
    }
}
