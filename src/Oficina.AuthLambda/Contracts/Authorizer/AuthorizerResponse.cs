using System.Text.Json.Serialization;

namespace Oficina.AuthLambda.Contracts.Authorizer;

public sealed class AuthorizerResponse
{
    [JsonPropertyName("isAuthorized")]
    public required bool IsAuthorized { get; init; }

    [JsonPropertyName("context")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AuthorizerContext? Context { get; init; }

    public static AuthorizerResponse Allow(AuthorizerContext context)
        => new()
        {
            IsAuthorized = true,
            Context = context
        };

    public static AuthorizerResponse Deny()
        => new()
        {
            IsAuthorized = false
        };
}
