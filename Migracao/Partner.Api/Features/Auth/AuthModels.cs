namespace Partner.Api.Features.Auth;

public sealed class AuthLoginRequest
{
    public string Login { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class AuthLoginResponse
{
    public string Status { get; init; } = "LOGIN_SUCCESS";
    public string? AccessToken { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public string? Name { get; init; }
    public string? PendingToken { get; init; }
    public string? Message { get; init; }
}

public sealed class AuthUser
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool IsActive { get; init; }
    public int FailedPasswordAttempts { get; init; }
    public DateTime? PasswordLockoutUntil { get; init; }
}
