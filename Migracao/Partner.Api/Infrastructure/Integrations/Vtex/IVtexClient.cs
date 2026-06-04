namespace Partner.Api.Infrastructure.Integrations.Vtex;

/// <summary>
/// Interface para cliente de integração com VTEX.
/// Responsável por operações de sincronização de dados e verificação de conectividade.
/// </summary>
public interface IVtexClient
{
    /// <summary>
    /// Verifica a conectividade com VTEX sem enviar dados.
    /// Realizado durante diagnóstico em IMP-8A.
    /// </summary>
    /// <param name="correlationId">ID para rastreabilidade</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da verificação de saúde</returns>
    Task<VtexHealthCheckResult> CheckConnectionAsync(
        string correlationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Sincroniza dados de cliente para VTEX (Master Data).
    /// Stub nesta fase IMP-8A: lança NotImplementedException
    /// Implementação real virá em IMP-8B+
    /// </summary>
    /// <param name="jobPublicId">ID público do job de importação</param>
    /// <param name="correlationId">ID para rastreabilidade</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da sincronização</returns>
    Task<VtexSyncResult> SyncClientAsync(
        Guid jobPublicId,
        string correlationId,
        CancellationToken cancellationToken);
}

/// <summary>
/// Resultado de uma operação de sincronização VTEX.
/// </summary>
public sealed record VtexSyncResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int? AttemptCount { get; init; }
    public long ElapsedMs { get; init; }
    public DateTime SyncedAtUtc { get; init; } = DateTime.UtcNow;
}

// VtexHealthCheckResult é definido em VtexHealthCheckResult.cs
