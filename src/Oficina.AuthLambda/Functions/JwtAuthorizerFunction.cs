using Amazon.Lambda.Core;
using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Contracts.Authorizer;

namespace Oficina.AuthLambda.Functions;

public sealed class JwtAuthorizerFunction
{
    private readonly JwtAuthorizerService _authorizer;

    public JwtAuthorizerFunction()
        : this(DependencyInjection.GetRequiredService<JwtAuthorizerService>())
    {
    }

    public JwtAuthorizerFunction(JwtAuthorizerService authorizer)
    {
        _authorizer = authorizer;
    }

    public AuthorizerResponse HandleAsync(HttpApiAuthorizerRequest request, ILambdaContext context)
    {
        try
        {
            return _authorizer.Authorize(GetAuthorizationHeader(request));
        }
        catch
        {
            return AuthorizerResponse.Deny();
        }
    }

    private static string? GetAuthorizationHeader(HttpApiAuthorizerRequest request)
    {
        if (request.Headers is null)
        {
            return null;
        }

        if (request.Headers.TryGetValue("Authorization", out var authorization) ||
            request.Headers.TryGetValue("authorization", out authorization))
        {
            return authorization;
        }

        return null;
    }
}
