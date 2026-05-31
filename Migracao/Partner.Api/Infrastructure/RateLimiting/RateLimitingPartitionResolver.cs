using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Partner.Api.Features.Auth;
using Partner.Api.Features.Auth.Mfa;
using Partner.Api.Infrastructure.Security;

namespace Partner.Api.Infrastructure.RateLimiting;

public static class RateLimitingPartitionResolver
{
    private const string Unknown = "unknown";

    public static string ResolveIp(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? Unknown;

    public static string ResolveLogin(HttpContext context)
    {
        if (!TryDeserialize<AuthLoginRequest>(context, out var request) || request is null)
        {
            return Unknown;
        }

        return Normalize(request.Login);
    }

    public static string ResolvePendingTokenId(HttpContext context)
    {
        if (!TryDeserialize<MfaVerifyRequest>(context, out var request) || request is null)
        {
            return Unknown;
        }

        if (string.IsNullOrWhiteSpace(request.PendingToken))
        {
            return Unknown;
        }

        var dot = request.PendingToken.IndexOf('.', StringComparison.Ordinal);
        if (dot <= 0)
        {
            return Unknown;
        }

        return request.PendingToken[..dot];
    }

    public static string ResolveUserId(HttpContext context)
        => context.User.FindFirst(PartnerClaimTypes.UserId)?.Value
            ?? context.User.FindFirst("sub")?.Value
            ?? Unknown;

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Unknown;
        }

        return value.Trim().ToLowerInvariant();
    }

    private static bool TryDeserialize<T>(HttpContext context, out T? value)
    {
        value = default;

        if (!context.Items.TryGetValue(RateLimitingMetadataKeys.RequestBody, out var stored))
        {
            return false;
        }

        if (stored is not string json || string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return value is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}