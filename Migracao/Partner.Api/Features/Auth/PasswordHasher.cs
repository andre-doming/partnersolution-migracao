using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace Partner.Api.Features.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private sealed class AuthPasswordUser;

    private static readonly AuthPasswordUser DummyUser = new();
    private readonly PasswordHasher<AuthPasswordUser> _identityHasher = new();

    private const string Prefix = "$pbkdf2-sha256$";

    public string Hash(string rawPassword)
    {
        return _identityHasher.HashPassword(DummyUser, rawPassword);
    }

    public bool Verify(string rawPassword, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        if (IsLegacyCustomModernHash(storedHash))
        {
            return VerifyLegacyCustomModernHash(rawPassword, storedHash);
        }

        var result = _identityHasher.VerifyHashedPassword(DummyUser, storedHash, rawPassword);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    public bool IsModernHash(string storedHash)
        => !string.IsNullOrWhiteSpace(storedHash)
           && (IsLegacyCustomModernHash(storedHash) || IsIdentityModernHash(storedHash));

    private static bool IsLegacyCustomModernHash(string storedHash)
        => storedHash.StartsWith(Prefix, StringComparison.Ordinal);

    private static bool IsIdentityModernHash(string storedHash)
        => storedHash.StartsWith("AQAAAA", StringComparison.Ordinal);

    private static bool VerifyLegacyCustomModernHash(string rawPassword, string storedHash)
    {
        var parts = storedHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4)
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var computed = Rfc2898DeriveBytes.Pbkdf2(rawPassword, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        return CryptographicOperations.FixedTimeEquals(computed, expected);
    }
}

