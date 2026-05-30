using System.Data;

namespace Partner.Api.Infrastructure.Database;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}
