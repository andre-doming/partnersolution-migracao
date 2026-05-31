using System.Security.Cryptography;
using System.Text;

namespace Partner.Api.Features.Auth.Mfa;

public static class MfaSecrets
{
    public static byte[] GenerateSecret(int size = 20)
    {
        var buffer = new byte[size];
        RandomNumberGenerator.Fill(buffer);
        return buffer;
    }
}

public static class Base32Encoding
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var output = new StringBuilder((data.Length + 4) / 5 * 8);
        var buffer = data[0];
        var next = 1;
        var bitsLeft = 8;

        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++];
                    bitsLeft += 8;
                }
                else
                {
                    buffer <<= 5 - bitsLeft;
                    bitsLeft = 5;
                }
            }

            var index = (buffer >> (bitsLeft - 5)) & 0x1f;
            bitsLeft -= 5;
            output.Append(Alphabet[index]);
        }

        return output.ToString();
    }

    public static byte[] Decode(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var cleaned = input.Trim().Replace("=", string.Empty).ToUpperInvariant();
        var buffer = 0;
        var bitsLeft = 0;
        var output = new List<byte>(cleaned.Length * 5 / 8);

        foreach (var c in cleaned)
        {
            var index = Alphabet.IndexOf(c);
            if (index < 0)
            {
                throw new FormatException("Invalid Base32 character.");
            }

            buffer = (buffer << 5) | index;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 0xff));
                bitsLeft -= 8;
            }
        }

        return output.ToArray();
    }
}

public static class TotpGenerator
{
    public static string GenerateCode(byte[] secret, DateTime timestamp, int digits = 6, int periodSeconds = 30)
    {
        var counter = GetCounter(timestamp, periodSeconds);
        var code = ComputeHotp(secret, counter);
        var mod = (int)Math.Pow(10, digits);
        var value = code % mod;
        return value.ToString().PadLeft(digits, '0');
    }

    public static bool ValidateCode(byte[] secret, string code, DateTime timestamp, int digits = 6, int periodSeconds = 30, int allowedDriftWindows = 1)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        for (var drift = -allowedDriftWindows; drift <= allowedDriftWindows; drift++)
        {
            var offset = timestamp.AddSeconds(drift * periodSeconds);
            var expected = GenerateCode(secret, offset, digits, periodSeconds);
            if (ConstantTimeEquals(expected, code.Trim()))
            {
                return true;
            }
        }

        return false;
    }

    private static long GetCounter(DateTime timestamp, int periodSeconds)
    {
        var unix = new DateTimeOffset(timestamp.ToUniversalTime()).ToUnixTimeSeconds();
        return unix / periodSeconds;
    }

    private static int ComputeHotp(byte[] key, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
                     | ((hash[offset + 1] & 0xff) << 16)
                     | ((hash[offset + 2] & 0xff) << 8)
                     | (hash[offset + 3] & 0xff);
        return binary;
    }

    private static bool ConstantTimeEquals(string left, string right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        var result = 0;
        for (var i = 0; i < left.Length; i++)
        {
            result |= left[i] ^ right[i];
        }

        return result == 0;
    }
}

public static class OtpAuthUriBuilder
{
    public static string Build(string issuer, string accountName, string base32Secret, int digits = 6, int periodSeconds = 30)
    {
        var label = Uri.EscapeDataString($"{issuer}:{accountName}");
        var encodedIssuer = Uri.EscapeDataString(issuer);
        return $"otpauth://totp/{label}?secret={base32Secret}&issuer={encodedIssuer}&digits={digits}&period={periodSeconds}";
    }
}

public sealed class RecoveryCodeService
{
    private const int DefaultCodeCount = 10;

    public IReadOnlyCollection<string> GenerateCodes(int count = DefaultCodeCount)
    {
        var codes = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            codes.Add(GenerateSingleCode());
        }

        return codes;
    }

    public string HashCode(string code)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        using var deriveBytes = new Rfc2898DeriveBytes(code, salt, 100_000, HashAlgorithmName.SHA256);
        var key = deriveBytes.GetBytes(32);
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(key);
    }

    public bool VerifyCode(string code, string storedHash)
    {
        var parts = storedHash.Split('.', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var expected = Convert.FromBase64String(parts[1]);
        using var deriveBytes = new Rfc2898DeriveBytes(code, salt, 100_000, HashAlgorithmName.SHA256);
        var actual = deriveBytes.GetBytes(32);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string GenerateSingleCode()
    {
        var buffer = RandomNumberGenerator.GetBytes(8);
        var code = BitConverter.ToString(buffer).Replace("-", string.Empty);
        return string.Create(14, code, (span, value) =>
        {
            span[0] = value[0];
            span[1] = value[1];
            span[2] = value[2];
            span[3] = value[3];
            span[4] = '-';
            span[5] = value[4];
            span[6] = value[5];
            span[7] = value[6];
            span[8] = value[7];
            span[9] = '-';
            span[10] = value[8];
            span[11] = value[9];
            span[12] = value[10];
            span[13] = value[11];
        });
    }
}

public sealed class PendingTokenService
{
    public string GenerateToken()
    {
        var buffer = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(buffer);
    }

    public byte[] HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
    }
}