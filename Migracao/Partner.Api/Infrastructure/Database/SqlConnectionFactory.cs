using Microsoft.Data.SqlClient;
using System.Data;

namespace Partner.Api.Infrastructure.Database;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PartnerDb")
            ?? throw new InvalidOperationException("ConnectionStrings:PartnerDb was not configured.");
    }

    public IDbConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
