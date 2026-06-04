namespace Partner.Api.Infrastructure.Import;

public static class ImportJobRequestFactory
{
    public static Partner.Api.Features.Import.ImportJobCreateRequest CreateQueued(
        Guid publicId,
        string feature,
        string fileName,
        string filePath,
        string fileHashSha256,
        int companyId,
        int createdByUserId,
        DateTime startedAtUtc,
        string? correlationId,
        int? retryOfImportJobId = null)
    {
        var createdAtUtc = DateTime.UtcNow;
        return new Partner.Api.Features.Import.ImportJobCreateRequest
        {
            PublicId = publicId,
            Feature = feature,
            FileName = fileName,
            FilePath = filePath,
            FileHashSha256 = fileHashSha256,
            CompanyId = companyId,
            CreatedByUserId = createdByUserId,
            Status = ImportJobStatus.Queued,
            TotalRows = 0,
            ProcessedRows = 0,
            SuccessRows = 0,
            ErrorRows = 0,
            DurationMs = 0,
            StartedAtUtc = startedAtUtc,
            CreatedAtUtc = createdAtUtc,
            FinishedAtUtc = null,
            CancelRequested = false,
            CancelRequestedAtUtc = null,
            CancelledAtUtc = null,
            RetryOfImportJobId = retryOfImportJobId,
            Attempts = 0,
            LastError = null,
            LockedBy = null,
            LockedAtUtc = null,
            LastHeartbeatAtUtc = null,
            CorrelationId = correlationId
        };
    }
}