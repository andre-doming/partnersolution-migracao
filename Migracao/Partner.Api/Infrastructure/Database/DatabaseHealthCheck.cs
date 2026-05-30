using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Data.Common;

namespace Partner.Api.Infrastructure.Database;

public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public DatabaseHealthCheck(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = (DbConnection)_connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);
            await connection.CloseAsync();
            return HealthCheckResult.Healthy("Database connection succeeded.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database connection failed.", exception);
        }
    }
}
