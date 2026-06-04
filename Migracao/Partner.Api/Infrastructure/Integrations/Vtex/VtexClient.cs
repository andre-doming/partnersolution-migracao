namespace Partner.Api.Infrastructure.Integrations.Vtex;

using Microsoft.Extensions.Options;

/// <summary>
/// Cliente HTTP para integração com VTEX.
/// Fase IMP-7: Implementação stub - lança NotImplementedException
/// Responsabilidades futuras:
/// - Configuração HTTP (AppKey/AppToken)
/// - Autenticação
/// - Tratamento de erros
/// - Retry parametrizável
/// </summary>
public sealed class VtexClient : IVtexClient
{
    private readonly VtexOptions _options;
    private readonly ILogger<VtexClient> _logger;

    public VtexClient(
        IOptions<VtexOptions> options,
        ILogger<VtexClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sincroniza dados de cliente para VTEX.
    /// Stub nesta fase - implementação real virá em fase futura (IMP-8+)
    /// </summary>
    public async Task<VtexSyncResult> SyncClientAsync(
        Guid jobPublicId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "VtexClient.SyncClientAsync STUB - Implementação futura. JobPublicId={JobPublicId} CorrelationId={CorrelationId}",
            jobPublicId,
            correlationId);

        // Stub - não fazer nada nesta fase
        throw new NotImplementedException(
            "VTEX sync será implementado em fase futura (IMP-8). " +
            "Esta versão é apenas infraestrutura com feature flag.");
    }
}
