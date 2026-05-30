namespace Partner.Api.Features.Import;

public sealed class ImportJobListRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class ImportJobListResponse
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int Total { get; init; }
    public IReadOnlyCollection<ImportJobItemResponse> Items { get; init; } = [];
}

public sealed class ImportJobItemResponse
{
    public int Id { get; init; }
    public string Feature { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int CompanyId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int SuccessRows { get; init; }
    public int ErrorRows { get; init; }
    public int DurationMs { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime? FinishedAtUtc { get; init; }
    public int CreatedByUserId { get; init; }
}

public sealed class ImportJobDetailResponse
{
    public int Id { get; init; }
    public string Feature { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int CompanyId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int SuccessRows { get; init; }
    public int ErrorRows { get; init; }
    public int DurationMs { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime? FinishedAtUtc { get; init; }
    public int CreatedByUserId { get; init; }
    public IReadOnlyCollection<ImportRowErrorResponse> Errors { get; init; } = [];
}

public sealed class ImportClientsCsvResponse
{
    public int JobId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int CompanyId { get; init; }
    public int TotalRows { get; init; }
    public int SuccessRows { get; init; }
    public int ErrorRows { get; init; }
    public int DurationMs { get; init; }
    public IReadOnlyCollection<ImportRowErrorResponse> Errors { get; init; } = [];
}

public sealed class ImportPreviewCsvResponse
{
    public string FileName { get; init; } = string.Empty;
    public int CompanyId { get; init; }
    public int TotalRows { get; init; }
    public int ValidRows { get; init; }
    public int InvalidRows { get; init; }
    public IReadOnlyCollection<ImportPreviewLineResponse> Lines { get; init; } = [];
}

public sealed class ImportPreviewLineResponse
{
    public int LineNumber { get; init; }
    public string Action { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;
    public string BirthDate { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public string? ValidationMessage { get; init; }
}

public sealed class ImportProcessSelectedRequest
{
    public int CompanyId { get; init; }
    public string FileName { get; init; } = "selected-lines.csv";
    public IReadOnlyCollection<ImportSelectedLineRequest> SelectedLines { get; init; } = [];
}

public sealed class ImportSelectedLineRequest
{
    public int LineNumber { get; init; }
    public string Action { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;
    public string BirthDate { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

public sealed class ImportRowErrorResponse
{
    public int LineNumber { get; init; }
    public string? Action { get; init; }
    public string? Document { get; init; }
    public string? Email { get; init; }
    public string Message { get; init; } = string.Empty;
}

internal sealed class ImportJobRow
{
    public int Id { get; init; }
    public string Feature { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public int CompanyId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalRows { get; init; }
    public int SuccessRows { get; init; }
    public int ErrorRows { get; init; }
    public int DurationMs { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime? FinishedAtUtc { get; init; }
    public int CreatedByUserId { get; init; }
}

internal sealed class ImportJobErrorRow
{
    public int LineNumber { get; init; }
    public string? Action { get; init; }
    public string? Document { get; init; }
    public string? Email { get; init; }
    public string Message { get; init; } = string.Empty;
}

internal sealed class ImportCompanyMapRow
{
    public int CompanyId { get; init; }
    public Guid PartnerId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
}

internal sealed class ImportExistingClientRow
{
    public int Id { get; init; }
}

internal sealed class CsvImportLineData
{
    public int LineNumber { get; init; }
    public string RawLine { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Gender { get; init; } = string.Empty;
    public string BirthDate { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

