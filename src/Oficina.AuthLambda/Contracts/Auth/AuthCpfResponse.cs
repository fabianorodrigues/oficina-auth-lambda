using System.Text.Json.Serialization;
using Oficina.AuthLambda.Application.Services;

namespace Oficina.AuthLambda.Contracts.Auth;

public sealed class AuthCpfResponse
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("expiresIn")]
    public required int ExpiresIn { get; init; }

    [JsonPropertyName("perfil")]
    public required string Perfil { get; init; }

    [JsonPropertyName("clienteId")]
    public Guid? ClienteId { get; init; }

    [JsonPropertyName("funcionarioId")]
    public Guid? FuncionarioId { get; init; }

    public static AuthCpfResponse FromResult(AuthResult result)
        => new()
        {
            AccessToken = result.AccessToken,
            ExpiresIn = result.ExpiresIn,
            Perfil = result.Perfil.ToString(),
            ClienteId = result.ClienteId,
            FuncionarioId = result.FuncionarioId
        };
}
