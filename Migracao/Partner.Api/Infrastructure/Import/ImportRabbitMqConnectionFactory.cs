using RabbitMQ.Client;

namespace Partner.Api.Infrastructure.Import;

public sealed class ImportRabbitMqConnectionFactory
{
    private readonly ImportRabbitMqOptions _options;

    public ImportRabbitMqConnectionFactory(ImportRabbitMqOptions options)
    {
        _options = options;
    }

    public IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            VirtualHost = _options.VirtualHost,
            UserName = _options.Username,
            Password = _options.Password,
            DispatchConsumersAsync = true
        };

        return factory.CreateConnection();
    }
}