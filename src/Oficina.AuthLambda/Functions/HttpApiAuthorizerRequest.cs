using System.Text.Json.Serialization;

namespace Oficina.AuthLambda.Functions;

public sealed class HttpApiAuthorizerRequest
{
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; init; }
}
