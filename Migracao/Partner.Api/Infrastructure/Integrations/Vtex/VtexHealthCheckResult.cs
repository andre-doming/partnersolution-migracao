namespace Partner.Api.Infrastructure.Integrations.Vtex;

/// <summary>
/// Status de saúde da conexão com VTEX.
/// </summary>
public enum VtexHealthStatus
{
    /// <summary>
    /// Conexão estabelecida com sucesso (HTTP 200).
    /// </summary>
    Connected = 0,

    /// <summary>
    /// Conexão falhou ou não respondeu.
    /// </summary>
    Disconnected = 1,

    /// <summary>
    /// Falha de autenticação (HTTP 401).
    /// </summary>
    Unauthorized = 2,

    /// <summary>
    /// Acesso proibido (HTTP 403).
    /// </summary>
    Forbidden = 3,

    /// <summary>
    /// Recurso não encontrado (HTTP 404).
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// Timeout na conectividade.
    /// </summary>
    Timeout = 5,

    /// <summary>
    /// Integração VTEX desabilitada pela feature flag.
    /// </summary>
    Disabled = 6
}

/// <summary>
/// Resultado de uma verificação de saúde da conexão com VTEX.
/// </summary>
public sealed record VtexHealthCheckResult
{
    /// <summary>
    /// Status da conexão.
    /// </summary>
    public VtexHealthStatus Status { get; init; }

    /// <summary>
    /// Tempo de resposta em milissegundos.
    /// </summary>
    public long ResponseTimeMs { get; init; }

    /// <summary>
    /// Status HTTP recebido (se aplicável).
    /// </summary>
    public int? StatusCode { get; init; }

    /// <summary>
    /// Mensagem descritiva (utilizada em erros).
    /// </summary>
    public string? Message { get; init; }
}
