namespace Partner.Api.Infrastructure.Integrations.Vtex;

using RabbitMQ.Client;
using Microsoft.Extensions.Options;

/// <summary>
/// Factory para criar conexões com RabbitMQ para fila VTEX.
/// Compartilha a mesma infraestrutura do Import mas usa configuração dedicada.
/// </summary>
public sealed class VtexRabbitMqConnectionFactory
{
    private readonly VtexRabbitMqOptions _options;
    private readonly ILogger<VtexRabbitMqConnectionFactory> _logger;

    public VtexRabbitMqConnectionFactory(
        VtexRabbitMqOptions options,
        ILogger<VtexRabbitMqConnectionFactory> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Cria uma nova conexão com RabbitMQ para sincronização VTEX.
    /// </summary>
    public IConnection CreateConnection()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                UserName = _options.Username,
                Password = _options.Password,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            var connection = factory.CreateConnection();

            _logger.LogInformation(
                "VtexRabbitMq connection established. Host={Host}:{Port} VHost={VHost}",
                _options.Host,
                _options.Port,
                _options.VirtualHost);

            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create VtexRabbitMq connection. Host={Host}:{Port}",
                _options.Host,
                _options.Port);
            throw;
        }
    }
}
