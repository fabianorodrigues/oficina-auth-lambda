using Amazon.Lambda.Core;
using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Contracts.Authorizer;

namespace Oficina.AuthLambda.Functions;

public sealed class JwtAuthorizerFunction
{
    private readonly JwtAuthorizerService _authorizer;

    public JwtAuthorizerFunction()
        : this(DependencyInjection.GetRequiredAuthorizerService<JwtAuthorizerService>())
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

        return request.Headers
            .FirstOrDefault(header => string.Equals(
                header.Key,
                "authorization",
                StringComparison.OrdinalIgnoreCase))
            .Value;
    }
}
