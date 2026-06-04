namespace Partner.Api.Infrastructure.Import;

public sealed class ImportRabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "partner";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Exchange { get; init; } = "partner.events";
    public string ImportJobsQueue { get; init; } = "partner.import.jobs";
    public string ImportDlqQueue { get; init; } = "partner.import.dlq";
}