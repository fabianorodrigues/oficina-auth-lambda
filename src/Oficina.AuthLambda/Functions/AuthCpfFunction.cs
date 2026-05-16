using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Contracts.Auth;
using Oficina.AuthLambda.Domain.Exceptions;
using Oficina.AuthLambda.Observability;

namespace Oficina.AuthLambda.Functions;

public sealed class AuthCpfFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AuthService _authService;

    public AuthCpfFunction()
        : this(DependencyInjection.GetRequiredAuthService<AuthService>())
    {
    }

    public AuthCpfFunction(AuthService authService)
    {
        _authService = authService;
    }

    public async Task<APIGatewayHttpApiV2ProxyResponse> HandleAsync(
        APIGatewayHttpApiV2ProxyRequest request,
        ILambdaContext context)
    {
        try
        {
            var authRequest = ParseRequest(request);
            var result = await _authService.AuthenticateAsync(authRequest, CancellationToken.None);
            SafeLambdaLogger.Log(context, "AutenticacaoCpf", "success", result.Perfil.ToString());
            return JsonResponse((int)HttpStatusCode.OK, AuthCpfResponse.FromResult(result));
        }
        catch (AuthException ex)
        {
            SafeLambdaLogger.Log(context, "AutenticacaoCpf", "failure", errorType: ex.GetType().Name);
            return JsonResponse(ex.StatusCode, new ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            SafeLambdaLogger.Log(context, "AutenticacaoCpf", "failure", errorType: ex.GetType().Name);
            return JsonResponse(
                (int)HttpStatusCode.InternalServerError,
                new ErrorResponse("Unexpected error."));
        }
    }

    private static AuthCpfRequest ParseRequest(APIGatewayHttpApiV2ProxyRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Body))
        {
            throw AuthException.BadRequest("Body is required.");
        }

        string body;
        try
        {
            body = request.IsBase64Encoded
                ? Encoding.UTF8.GetString(Convert.FromBase64String(request.Body))
                : request.Body;
        }
        catch (FormatException)
        {
            throw AuthException.BadRequest("Invalid body.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw AuthException.BadRequest("Body is required.");
        }

        try
        {
            return JsonSerializer.Deserialize<AuthCpfRequest>(body, JsonOptions)
                   ?? throw AuthException.BadRequest("Invalid body.");
        }
        catch (JsonException)
        {
            throw AuthException.BadRequest("Malformed JSON.");
        }
    }

    private static APIGatewayHttpApiV2ProxyResponse JsonResponse<T>(int statusCode, T body)
        => new()
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                ["content-type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(body, JsonOptions)
        };
}
