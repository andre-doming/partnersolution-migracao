namespace Partner.Api.Features.Clients;

public sealed class ClientListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Field { get; init; }
    public string? Value { get; init; }
    public int? CompanyId { get; init; }
    public string? Active { get; init; }
}

public sealed class ClientUpsertRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Gender { get; init; }
    public string? BirthDate { get; init; }
    public int CompanyId { get; init; }
    public string? Department { get; init; }
    public string? Role { get; init; }
    public bool Approved { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class ClientListResponse
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public IReadOnlyCollection<ClientListItemResponse> Items { get; init; } = [];
}

public sealed class ClientListItemResponse
{
    public int Id { get; init; }
    public Guid ClientGuid { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Gender { get; init; }
    public string? BirthDate { get; init; }
    public int CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? Role { get; init; }
    public bool Approved { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ClientDetailResponse
{
    public int Id { get; init; }
    public Guid ClientGuid { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Gender { get; init; }
    public string? BirthDate { get; init; }
    public int CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? Role { get; init; }
    public bool Approved { get; init; }
    public bool IsActive { get; init; }
}

public sealed class ClientLookupResponse
{
    public IReadOnlyCollection<ClientCompanyLookupItem> Companies { get; init; } = [];
}

public sealed class ClientCompanyLookupItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

internal sealed class ClientRow
{
    public int Id { get; init; }
    public Guid ClientGuid { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Gender { get; init; }
    public string? BirthDate { get; init; }
    public int CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? Role { get; init; }
    public bool Approved { get; init; }
    public bool IsActive { get; init; }
}

internal sealed class ClientCompanyMapRow
{
    public int CompanyId { get; init; }
    public Guid PartnerId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
}

