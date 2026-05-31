namespace Partner.Api.Infrastructure.RateLimiting;

public static class RateLimitingPolicyNames
{
    public const string AuthLogin = "auth-login";
    public const string AuthMfaVerify = "auth-mfa-verify";
    public const string AuthMfaActivate = "auth-mfa-activate";
    public const string AuthMfaSetup = "auth-mfa-setup";
    public const string AdminMfaReset = "admin-mfa-reset";
    public const string AdminMfaUnlock = "admin-mfa-unlock";
}

public static class RateLimitingMetadataKeys
{
    public const string RequestBody = "RateLimit.RequestBody";
}

public sealed class RateLimitPolicyMetadata
{
    public RateLimitPolicyMetadata(string policyName)
    {
        PolicyName = policyName;
    }

    public string PolicyName { get; }
}