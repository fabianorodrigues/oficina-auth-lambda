using Amazon.Lambda.Core;
using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Contracts.Authorizer;
using Oficina.AuthLambda.Observability;

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
            var response = _authorizer.Authorize(GetAuthorizationHeader(request));
            SafeLambdaLogger.Log(context, "JwtAuthorizer", response.IsAuthorized ? "allow" : "deny");
            return response;
        }
        catch (Exception ex)
        {
            SafeLambdaLogger.Log(context, "JwtAuthorizer", "failure", errorType: ex.GetType().Name);
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
