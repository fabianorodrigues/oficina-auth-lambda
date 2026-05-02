using Microsoft.Data.SqlClient;

namespace Oficina.AuthLambda.Application.Abstractions;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken);
}
