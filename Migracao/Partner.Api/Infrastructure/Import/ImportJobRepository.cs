using Dapper;
using Partner.Api.Features.Import;
using Partner.Api.Infrastructure.Database;

namespace Partner.Api.Infrastructure.Import;

public sealed class ImportJobRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ImportJobRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ImportJobDetail?> GetByPublicIdAsync(Guid publicId, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        var job = await connection.QueryFirstOrDefaultAsync<ImportJobDetail>(new CommandDefinition(
            ImportQueries.GetImportJobByPublicId,
            new { PublicId = publicId },
            cancellationToken: cancellationToken));

        return job;
    }

    public async Task<int> MarkRunningAsync(int jobId, string lockedBy, DateTime lockedAtUtc, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        return await connection.ExecuteAsync(new CommandDefinition(
            ImportQueries.MarkImportJobRunning,
            new
            {
                Id = jobId,
                Status = ImportJobStatus.Running,
                LockedBy = lockedBy,
                LockedAtUtc = lockedAtUtc,
                ExpectedStatus = ImportJobStatus.Queued
            },
            cancellationToken: cancellationToken));
    }

    public async Task UpdateStatusAsync(int jobId, string status, DateTime finishedAtUtc, int durationMs, string? lastError, CancellationToken cancellationToken)
    {
        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            ImportQueries.UpdateImportJobStatus,
            new
            {
                Id = jobId,
                Status = status,
                FinishedAtUtc = finishedAtUtc,
                DurationMs = durationMs,
                LastError = lastError,
                CancelledAtUtc = (DateTime?)null,
                CancelRequested = false,
                CancelRequestedAtUtc = (DateTime?)null
            },
            cancellationToken: cancellationToken));
    }
}

public sealed class ImportJobDetail
{
    public int Id { get; init; }
    public Guid PublicId { get; init; }
    public string Feature { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public string FileHashSha256 { get; init; } = string.Empty;
    public int CompanyId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int? TotalRows { get; init; }
    public int ProcessedRows { get; init; }
    public int SuccessRows { get; init; }
    public int ErrorRows { get; init; }
    public int DurationMs { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime? FinishedAtUtc { get; init; }
    public int CreatedByUserId { get; init; }
    public int Attempts { get; init; }
    public string? LastError { get; init; }
    public string? CorrelationId { get; init; }
    public bool CancelRequested { get; init; }
    public DateTime? CancelRequestedAtUtc { get; init; }
    public DateTime? CancelledAtUtc { get; init; }
    public int? RetryOfImportJobId { get; init; }
}