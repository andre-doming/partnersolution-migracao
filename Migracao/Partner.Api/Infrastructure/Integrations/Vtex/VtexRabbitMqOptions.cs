namespace Partner.Api.Infrastructure.Integrations.Vtex;

/// <summary>
/// Configuração de RabbitMQ para sincronização com VTEX.
/// Usa fila dedicada: partner.vtex.sync
/// </summary>
public sealed class VtexRabbitMqOptions
{
    public const string SectionName = "VtexRabbitMq";

    /// <summary>
    /// Host do RabbitMQ
    /// </summary>
    public string Host { get; init; } = "localhost";

    /// <summary>
    /// Porta do RabbitMQ
    /// Padrão: 5672
    /// </summary>
    public int Port { get; init; } = 5672;

    /// <summary>
    /// Virtual Host do RabbitMQ
    /// Padrão: partner
    /// </summary>
    public string VirtualHost { get; init; } = "partner";

    /// <summary>
    /// Usuário para conexão RabbitMQ
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Senha para conexão RabbitMQ
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Exchange do RabbitMQ
    /// Padrão: partner.events
    /// </summary>
    public string Exchange { get; init; } = "partner.events";

    /// <summary>
    /// Nome da fila de sincronização VTEX
    /// Padrão: partner.vtex.sync
    /// </summary>
    public string VtexSyncQueue { get; init; } = "partner.vtex.sync";

    /// <summary>
    /// Nome da fila de dead-letter (falhas persistentes)
    /// Padrão: partner.vtex.dlq
    /// </summary>
    public string VtexDlqQueue { get; init; } = "partner.vtex.dlq";
}
