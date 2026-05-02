using Oficina.AuthLambda.Contracts.Authorizer;

namespace Oficina.AuthLambda.Application.Services;

public sealed record JwtValidationResult(bool IsValid, AuthorizerContext? Context)
{
    public static JwtValidationResult Valid(AuthorizerContext context) => new(true, context);

    public static JwtValidationResult Invalid() => new(false, null);
}
