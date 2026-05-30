namespace Partner.Api.Features.Auth;

public sealed class AuthLoginRequest
{
    public string Login { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class AuthLoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class AuthUser
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool IsActive { get; init; }
}
