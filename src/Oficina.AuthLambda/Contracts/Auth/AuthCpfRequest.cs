using System.Text.Json.Serialization;

namespace Oficina.AuthLambda.Contracts.Auth;

public sealed class AuthCpfRequest
{
    [JsonPropertyName("cpf")]
    public string? Cpf { get; init; }

    [JsonPropertyName("senha")]
    public string? Senha { get; init; }
}
