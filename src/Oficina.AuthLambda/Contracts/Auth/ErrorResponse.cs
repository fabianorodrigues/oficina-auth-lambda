using System.Text.Json.Serialization;

namespace Oficina.AuthLambda.Contracts.Auth;

public sealed record ErrorResponse([property: JsonPropertyName("erro")] string Erro);
