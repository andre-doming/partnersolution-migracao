namespace Partner.Api.Features.Auth;

public sealed class PasswordLockoutOptions
{
    public const string SectionName = "PasswordLockout";

    public int PasswordMaxAttempts { get; init; } = 5;
    public int PasswordLockoutMinutes { get; init; } = 15;
}