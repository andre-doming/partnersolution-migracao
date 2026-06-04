namespace Partner.Api.Infrastructure.Import;

public sealed class ImportJobMessage
{
    public int SchemaVersion { get; init; } = 1;
    public string MessageId { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public ImportJobPayload Job { get; init; } = new();
}

public sealed class ImportJobPayload
{
    public Guid PublicId { get; init; }
    public int InternalId { get; init; }
    public string Feature { get; init; } = string.Empty;
    public int CompanyId { get; init; }
    public int CreatedByUserId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string FileHashSha256 { get; init; } = string.Empty;
    public DateTime QueuedAtUtc { get; init; }
}