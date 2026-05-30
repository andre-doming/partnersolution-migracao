namespace Partner.Api.Features.Companies;

public sealed class CompanyListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Field { get; init; }
    public string? Value { get; init; }
    public string? Active { get; init; }
}

public sealed class CompanyUpsertRequest
{
    public string Cnpj { get; init; } = string.Empty;
    public string TradeName { get; init; } = string.Empty;
    public string CorporateName { get; init; } = string.Empty;
    public string ManagerName { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public sealed class CompanyListResponse
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public IReadOnlyCollection<CompanyListItemResponse> Items { get; init; } = [];
}

public sealed class CompanyListItemResponse
{
    public int Id { get; init; }
    public string Cnpj { get; init; } = string.Empty;
    public string TradeName { get; init; } = string.Empty;
    public string CorporateName { get; init; } = string.Empty;
    public string ManagerName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

public sealed class CompanyDetailResponse
{
    public int Id { get; init; }
    public string Cnpj { get; init; } = string.Empty;
    public string TradeName { get; init; } = string.Empty;
    public string CorporateName { get; init; } = string.Empty;
    public string ManagerName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

internal sealed class CompanyRow
{
    public int Id { get; init; }
    public string Cnpj { get; init; } = string.Empty;
    public string TradeName { get; init; } = string.Empty;
    public string CorporateName { get; init; } = string.Empty;
    public string ManagerName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
}

