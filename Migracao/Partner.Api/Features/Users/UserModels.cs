namespace Partner.Api.Features.Users;

public sealed class UserListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Field { get; init; }
    public string? Value { get; init; }
    public int? CompanyId { get; init; }
    public string? Active { get; init; }
}

public sealed class UserUpsertRequest
{
    public string Name { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public string? Password { get; init; }
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool AccessToken { get; init; }
    public bool IsActive { get; init; } = true;
    public IReadOnlyCollection<int> CompanyIds { get; init; } = [];
    public IReadOnlyCollection<int> FunctionIds { get; init; } = [];
}

public sealed class UserListResponse
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public IReadOnlyCollection<UserListItemResponse> Items { get; init; } = [];
}

public sealed class UserListItemResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool IsActive { get; init; }
    public bool AccessToken { get; init; }
    public bool? MfaEnabled { get; init; }
    public DateTime? MfaConfiguredAt { get; init; }
    public DateTime? LastSuccessfulMfaAt { get; init; }
    public bool? MfaResetRequired { get; init; }
    public int? FailedPasswordAttempts { get; init; }
    public DateTime? PasswordLockoutUntil { get; init; }
    public int? FailedMfaAttempts { get; init; }
    public DateTime? MfaLockoutUntil { get; init; }
    public IReadOnlyCollection<int> CompanyIds { get; set; } = [];
    public IReadOnlyCollection<string> Permissions { get; set; } = [];
}

public sealed class UserDetailResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool IsActive { get; init; }
    public bool AccessToken { get; init; }
    public bool FirstAccess { get; init; }
    public IReadOnlyCollection<int> CompanyIds { get; init; } = [];
    public IReadOnlyCollection<int> FunctionIds { get; init; } = [];
}

public sealed class UserLookupResponse
{
    public IReadOnlyCollection<UserCompanyLookupItem> Companies { get; init; } = [];
    public IReadOnlyCollection<UserFunctionLookupItem> Functions { get; init; } = [];
}

public sealed class UserCompanyLookupItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class UserFunctionLookupItem
{
    public int Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

internal sealed class UserListRow
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool IsActive { get; init; }
    public bool AccessToken { get; init; }
    public bool? MfaEnabled { get; init; }
    public DateTime? MfaConfiguredAt { get; init; }
    public DateTime? LastSuccessfulMfaAt { get; init; }
    public bool? MfaResetRequired { get; init; }
    public int? FailedPasswordAttempts { get; init; }
    public DateTime? PasswordLockoutUntil { get; init; }
    public int? FailedMfaAttempts { get; init; }
    public DateTime? MfaLockoutUntil { get; init; }
}

internal sealed class UserDetailRow
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Login { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool IsActive { get; init; }
    public bool AccessToken { get; init; }
    public bool FirstAccess { get; init; }
}

internal sealed class UserIdLinkRow
{
    public int UserId { get; init; }
    public int Id { get; init; }
}

internal sealed class UserPermissionLinkRow
{
    public int UserId { get; init; }
    public string Code { get; init; } = string.Empty;
}


