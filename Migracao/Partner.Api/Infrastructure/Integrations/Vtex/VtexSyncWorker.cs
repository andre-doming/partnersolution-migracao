namespace Partner.Api.Infrastructure.Integrations.Vtex;

using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

/// <summary>
/// Worker para processar mensagens de sincronização VTEX da fila vtex.sync.
/// Fase IMP-7: Apenas consome mensagens e registra logs.
/// Implementação real virá em fase futura (IMP-8+).
/// </summary>
public sealed class VtexSyncWorker : BackgroundService
{
    private readonly VtexRabbitMqConnectionFactory _connectionFactory;
    private readonly VtexRabbitMqOptions _rabbitOptions;
    private readonly VtexOptions _vtexOptions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VtexSyncWorker> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public VtexSyncWorker(
        VtexRabbitMqConnectionFactory connectionFactory,
        IOptions<VtexRabbitMqOptions> rabbitOptions,
        IOptions<VtexOptions> vtexOptions,
        IServiceScopeFactory scopeFactory,
        ILogger<VtexSyncWorker> logger)
    {
        _connectionFactory = connectionFactory;
        _rabbitOptions = rabbitOptions.Value;
        _vtexOptions = vtexOptions.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declarar exchange
            _channel.ExchangeDeclare(
                _rabbitOptions.Exchange,
                ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declarar filas com DLQ
            var queueArguments = new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = _rabbitOptions.Exchange,
                ["x-dead-letter-routing-key"] = VtexSyncPublisher.VtexDlqRoutingKey
            };

            _channel.QueueDeclare(
                _rabbitOptions.VtexSyncQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: queueArguments);

            _channel.QueueDeclare(
                _rabbitOptions.VtexDlqQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind filas
            _channel.QueueBind(
                _rabbitOptions.VtexSyncQueue,
                _rabbitOptions.Exchange,
                VtexSyncPublisher.VtexSyncRoutingKey);

            _channel.QueueBind(
                _rabbitOptions.VtexDlqQueue,
                _rabbitOptions.Exchange,
                VtexSyncPublisher.VtexDlqRoutingKey);

            // QoS: processar 1 mensagem por vez
            _channel.BasicQos(0, 1, false);

            // Criar consumer
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceivedAsync;

            // Começar a consumir
            _channel.BasicConsume(
                queue: _rabbitOptions.VtexSyncQueue,
                autoAck: false,
                consumer: consumer);

            _logger.LogInformation(
                "VtexSyncWorker started. Feature flag enabled: {Enabled}. Queue: {Queue}",
                _vtexOptions.Enabled,
                _rabbitOptions.VtexSyncQueue);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VtexSyncWorker failed to start");
            throw;
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs args)
    {
        if (_channel is null)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var messageJson = Encoding.UTF8.GetString(args.Body.ToArray());
            var message = JsonSerializer.Deserialize<VtexSyncMessage>(messageJson);

            if (message is null)
            {
                _logger.LogWarning("VtexSync received null message, ACKing");
                _channel.BasicAck(args.DeliveryTag, false);
                return;
            }

            var correlationId = message.CorrelationId ?? string.Empty;

            // Log: mensagem recebida
            _logger.LogInformation(
                "VtexSyncReceived CorrelationId={CorrelationId} JobPublicId={JobPublicId} JobId={JobId}",
                correlationId,
                message.JobPublicId,
                message.JobId);

            // Feature flag desligada
            if (!_vtexOptions.Enabled)
            {
                _logger.LogInformation(
                    "VtexSyncSkippedByFeatureFlag CorrelationId={CorrelationId} JobPublicId={JobPublicId}",
                    correlationId,
                    message.JobPublicId);

                _channel.BasicAck(args.DeliveryTag, false);
                stopwatch.Stop();
                return;
            }

            // Fase IMP-7: apenas registrar evento, não processar
            _logger.LogInformation(
                "VtexSyncWorker phase IMP-7: stub mode. CorrelationId={CorrelationId} JobPublicId={JobPublicId}",
                correlationId,
                message.JobPublicId);

            // ACK mensagem (não será reprocessada)
            _channel.BasicAck(args.DeliveryTag, false);

            stopwatch.Stop();
            _logger.LogInformation(
                "VtexSync message processed (stub). ElapsedMs={ElapsedMs}",
                stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing VtexSync message. NACK will be sent for requeue.");

            stopwatch.Stop();

            // NACK para reprocessar
            try
            {
                _channel.BasicNack(args.DeliveryTag, false, requeue: true);
            }
            catch (Exception nackEx)
            {
                _logger.LogError(nackEx, "Failed to NACK message");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("VtexSyncWorker stopping");

        _channel?.Dispose();
        _connection?.Dispose();

        await base.StopAsync(cancellationToken);

        _logger.LogInformation("VtexSyncWorker stopped");
    }
}
