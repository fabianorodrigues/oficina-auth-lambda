using System.Text.Json.Serialization;
using Oficina.AuthLambda.Domain.Enums;

namespace Oficina.AuthLambda.Contracts.Authorizer;

public sealed class AuthorizerContext
{
    [JsonPropertyName("cpf")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Cpf { get; init; }

    [JsonPropertyName("role")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; init; }

    [JsonPropertyName("clienteId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClienteId { get; init; }

    [JsonPropertyName("funcionarioId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FuncionarioId { get; init; }

    public static AuthorizerContext Cliente(string cpf, Guid clienteId)
        => new()
        {
            Cpf = cpf,
            Role = PerfilAcesso.Cliente.ToString(),
            ClienteId = clienteId.ToString()
        };

    public static AuthorizerContext Funcionario(string cpf, PerfilAcesso perfil, Guid funcionarioId)
        => new()
        {
            Cpf = cpf,
            Role = perfil.ToString(),
            FuncionarioId = funcionarioId.ToString()
        };
}
