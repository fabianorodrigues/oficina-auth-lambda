using Microsoft.Data.SqlClient;
using Oficina.AuthLambda.Application.Abstractions;
using Oficina.AuthLambda.Configuration;

namespace Oficina.AuthLambda.Infrastructure.Database;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly DatabaseOptions _options;

    public SqlConnectionFactory(DatabaseOptions options)
    {
        _options = options;
    }

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
