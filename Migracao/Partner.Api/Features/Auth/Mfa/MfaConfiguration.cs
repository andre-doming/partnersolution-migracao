namespace Partner.Api.Features.Auth.Mfa;

public sealed class MfaOptions
{
    public const string SectionName = "Mfa";

    public int MfaPendingTokenMinutes { get; init; } = 10;
    public int MfaMaxAttempts { get; init; } = 5;
    public int MfaLockoutMinutes { get; init; } = 15;
}