using RabbitMQ.Client;
using System.Text.Json;

namespace Partner.Api.Infrastructure.Import;

public sealed record ImportJobPublishCommand(
    string Exchange,
    string RoutingKey,
    ReadOnlyMemory<byte> Body,
    string MessageId,
    string CorrelationId);

public static class ImportJobPublisher
{
    public const string JobQueuedRoutingKey = "partner.import.job.queued";
    public const string JobDlqRoutingKey = "partner.import.job.dlq";

    public static ImportJobPublishCommand BuildPublishCommand(ImportRabbitMqOptions options, ImportJobMessage message)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        return new ImportJobPublishCommand(
            options.Exchange,
            JobQueuedRoutingKey,
            body,
            message.MessageId,
            message.CorrelationId);
    }

    public static void Publish(
        ImportRabbitMqConnectionFactory connectionFactory,
        ImportRabbitMqOptions options,
        ImportJobMessage message)
    {
        var command = BuildPublishCommand(options, message);

        using var connection = connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(command.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        var queueArguments = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = command.Exchange,
            ["x-dead-letter-routing-key"] = JobDlqRoutingKey
        };

        channel.QueueDeclare(options.ImportJobsQueue, durable: true, exclusive: false, autoDelete: false, arguments: queueArguments);
        channel.QueueDeclare(options.ImportDlqQueue, durable: true, exclusive: false, autoDelete: false);

        channel.QueueBind(options.ImportJobsQueue, command.Exchange, command.RoutingKey);
        channel.QueueBind(options.ImportDlqQueue, command.Exchange, JobDlqRoutingKey);

        var props = channel.CreateBasicProperties();
        props.MessageId = command.MessageId;
        props.CorrelationId = command.CorrelationId;
        props.Persistent = true;
        props.ContentType = "application/json";

        channel.BasicPublish(command.Exchange, command.RoutingKey, props, command.Body);
    }
}