using System.Security.Cryptography;

namespace Partner.Api.Infrastructure.Import;

public static class ImportFileStorage
{
    public static async Task<string> SaveStreamAndComputeHashAsync(Stream input, string fullPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var output = File.Create(fullPath);
        using var sha256 = SHA256.Create();

        var buffer = new byte[81920];
        int bytesRead;
        while ((bytesRead = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>()).ToLowerInvariant();
    }

    public static string ComputeSha256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(data)).ToLowerInvariant();
    }
}