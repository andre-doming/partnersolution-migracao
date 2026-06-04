namespace Partner.Api.Infrastructure.Integrations.Vtex;

using RabbitMQ.Client;
using System.Text.Json;

/// <summary>
/// Publisher de mensagens de sincronização VTEX.
/// Responsável por publicar na fila vtex.sync quando uma importação é concluída.
/// Respeita a feature flag IMPORT_VTEX_ENABLED.
/// </summary>
public static class VtexSyncPublisher
{
    /// <summary>
    /// Routing key para mensagens de sincronização VTEX
    /// </summary>
    public const string VtexSyncRoutingKey = "partner.vtex.sync";

    /// <summary>
    /// Routing key para dead-letter queue
    /// </summary>
    public const string VtexDlqRoutingKey = "partner.vtex.dlq";

    /// <summary>
    /// Publica uma mensagem de sincronização VTEX se a feature flag estiver ativa.
    /// Se desligada, apenas registra log e retorna sem publicar.
    /// </summary>
    /// <param name="vtexOptions">Configuração VTEX (contém feature flag)</param>
    /// <param name="connectionFactory">Factory para criar conexão RabbitMQ</param>
    /// <param name="rabbitOptions">Configuração RabbitMQ</param>
    /// <param name="message">Mensagem a publicar</param>
    /// <param name="logger">Logger</param>
    public static void PublishIfEnabled(
        VtexOptions vtexOptions,
        VtexRabbitMqConnectionFactory connectionFactory,
        VtexRabbitMqOptions rabbitOptions,
        VtexSyncMessage message,
        ILogger logger)
    {
        if (!vtexOptions.Enabled)
        {
            logger.LogInformation(
                "VtexSyncSkippedByFeatureFlag CorrelationId={CorrelationId} JobPublicId={JobPublicId} JobId={JobId}",
                message.CorrelationId,
                message.JobPublicId,
                message.JobId);
            return;
        }

        try
        {
            PublishMessage(connectionFactory, rabbitOptions, message, logger);

            logger.LogInformation(
                "VtexSyncQueued CorrelationId={CorrelationId} JobPublicId={JobPublicId} JobId={JobId}",
                message.CorrelationId,
                message.JobPublicId,
                message.JobId);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to publish VtexSync message. CorrelationId={CorrelationId} JobPublicId={JobPublicId}",
                message.CorrelationId,
                message.JobPublicId);
            throw;
        }
    }

    private static void PublishMessage(
        VtexRabbitMqConnectionFactory connectionFactory,
        VtexRabbitMqOptions rabbitOptions,
        VtexSyncMessage message,
        ILogger logger)
    {
        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        // Declarar exchange
        channel.ExchangeDeclare(
            rabbitOptions.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        // Definir arguments para DLQ
        var queueArguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = rabbitOptions.Exchange,
            ["x-dead-letter-routing-key"] = VtexDlqRoutingKey
        };

        // Declarar filas
        channel.QueueDeclare(
            rabbitOptions.VtexSyncQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments);

        channel.QueueDeclare(
            rabbitOptions.VtexDlqQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // Bind filas ao exchange
        channel.QueueBind(
            rabbitOptions.VtexSyncQueue,
            rabbitOptions.Exchange,
            VtexSyncRoutingKey);

        channel.QueueBind(
            rabbitOptions.VtexDlqQueue,
            rabbitOptions.Exchange,
            VtexDlqRoutingKey);

        // Preparar propriedades da mensagem
        var props = channel.CreateBasicProperties();
        props.MessageId = message.MessageId;
        props.CorrelationId = message.CorrelationId;
        props.Persistent = true;
        props.ContentType = "application/json";

        // Serializar e publicar
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        channel.BasicPublish(
            rabbitOptions.Exchange,
            VtexSyncRoutingKey,
            props,
            body);
    }
}
