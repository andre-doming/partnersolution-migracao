namespace Partner.Api.Infrastructure.Integrations.Vtex;

/// <summary>
/// Configuração de integração com VTEX.
/// Feature flag IMPORT_VTEX_ENABLED controla se a sincronização está ativa.
/// Na fase IMP-7, todos os dados são capturados mas não enviados para VTEX.
/// </summary>
public sealed class VtexOptions
{
    public const string SectionName = "Vtex";

    /// <summary>
    /// Feature flag global: habilita/desabilita integração com VTEX
    /// Padrão: false (desligado)
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// URL base da API VTEX
    /// Exemplo: https://api.vtex.com/lojabestoff
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Chave de aplicação VTEX (via variável de ambiente)
    /// </summary>
    public string AppKey { get; init; } = string.Empty;

    /// <summary>
    /// Token de aplicação VTEX (via variável de ambiente)
    /// </summary>
    public string AppToken { get; init; } = string.Empty;

    /// <summary>
    /// Número de tentativas em caso de falha
    /// Padrão: 3
    /// </summary>
    public int RetryCount { get; init; } = 3;

    /// <summary>
    /// Intervalo entre tentativas em milissegundos
    /// Padrão: 1000ms
    /// </summary>
    public int RetryDelayMs { get; init; } = 1000;
}
