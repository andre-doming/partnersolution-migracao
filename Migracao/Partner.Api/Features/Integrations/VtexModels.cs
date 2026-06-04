namespace Partner.Api.Features.Integrations;

/// <summary>
/// Resposta do endpoint de verificação de saúde VTEX.
/// Fornece informações sobre conectividade sem expor credenciais.
/// </summary>
public sealed record VtexHealthResponse
{
    /// <summary>
    /// Indica se a integração VTEX está habilitada pela feature flag.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Status da conexão: Connected, Disconnected, Unauthorized, Forbidden, NotFound, Timeout, Disabled.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// URL base da VTEX (parcialmente mascarada por segurança).
    /// Exemplo: https://***/lojabestoff
    /// Null quando disabled.
    /// </summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// Tempo de resposta em milissegundos.
    /// Zero quando disabled.
    /// </summary>
    public long ResponseTimeMs { get; init; }

    /// <summary>
    /// Timestamp UTC da verificação.
    /// </summary>
    public string TimestampUtc { get; init; } = DateTime.UtcNow.ToString("O");

    /// <summary>
    /// Mensagem descritiva (apenas em caso de erro).
    /// </summary>
    public string? Message { get; init; }
}
