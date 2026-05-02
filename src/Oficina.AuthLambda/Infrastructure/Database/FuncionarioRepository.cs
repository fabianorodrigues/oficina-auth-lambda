using Microsoft.Data.SqlClient;
using Oficina.AuthLambda.Application.Abstractions;
using Oficina.AuthLambda.Domain.Enums;
using Oficina.AuthLambda.Domain.Models;
using Oficina.AuthLambda.Domain.ValueObjects;

namespace Oficina.AuthLambda.Infrastructure.Database;

public sealed class FuncionarioRepository : IFuncionarioRepository
{
    private const string Query = """
        SELECT TOP (1) Id, Nome, Cpf, SenhaHash, Perfil, Ativo
        FROM Funcionarios
        WHERE Cpf = @Cpf
        """;

    private readonly ISqlConnectionFactory _connectionFactory;

    public FuncionarioRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<FuncionarioAuth?> ObterPorCpfAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(Query, connection);
        command.Parameters.AddWithValue("@Cpf", cpf.Valor);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new FuncionarioAuth(
            reader.GetGuid(0),
            reader.GetString(1),
            Cpf.Parse(reader.GetString(2)),
            reader.GetString(3),
            MapPerfil(reader.GetInt32(4)),
            reader.GetBoolean(5));
    }

    private static PerfilAcesso MapPerfil(int perfil) => perfil switch
    {
        1 => PerfilAcesso.Funcionario,
        2 => PerfilAcesso.Admin,
        _ => throw new InvalidOperationException("Invalid employee profile.")
    };
}
