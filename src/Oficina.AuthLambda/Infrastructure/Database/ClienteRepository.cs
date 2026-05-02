using Microsoft.Data.SqlClient;
using Oficina.AuthLambda.Application.Abstractions;
using Oficina.AuthLambda.Domain.Models;
using Oficina.AuthLambda.Domain.ValueObjects;

namespace Oficina.AuthLambda.Infrastructure.Database;

public sealed class ClienteRepository : IClienteRepository
{
    private const string Query = """
        SELECT TOP (1) Id, Documento
        FROM Clientes
        WHERE Documento = @Documento
        """;

    private readonly ISqlConnectionFactory _connectionFactory;

    public ClienteRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<ClienteAuth?> ObterPorCpfAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(Query, connection);
        command.Parameters.AddWithValue("@Documento", cpf.Valor);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new ClienteAuth(
            reader.GetGuid(0),
            Cpf.Parse(reader.GetString(1)),
            Ativo: true);
    }
}
