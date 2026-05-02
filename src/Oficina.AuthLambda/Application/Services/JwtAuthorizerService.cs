using Oficina.AuthLambda.Contracts.Authorizer;

namespace Oficina.AuthLambda.Application.Services;

public sealed class JwtAuthorizerService
{
    private readonly Abstractions.IJwtTokenService _jwt;

    public JwtAuthorizerService(Abstractions.IJwtTokenService jwt)
    {
        _jwt = jwt;
    }

    public AuthorizerResponse Authorize(string? authorizationHeader)
    {
        if (!TryGetBearerToken(authorizationHeader, out var token))
        {
            return AuthorizerResponse.Deny();
        }

        var validation = _jwt.ValidateToken(token);

        return validation.IsValid && validation.Context is not null
            ? AuthorizerResponse.Allow(validation.Context)
            : AuthorizerResponse.Deny();
    }

    private static bool TryGetBearerToken(string? authorizationHeader, out string token)
    {
        token = string.Empty;
        const string bearerPrefix = "Bearer ";

        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !authorizationHeader.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = authorizationHeader[bearerPrefix.Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }
}
