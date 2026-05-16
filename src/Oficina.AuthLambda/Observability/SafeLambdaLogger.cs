using System.Text.Json;
using Amazon.Lambda.Core;

namespace Oficina.AuthLambda.Observability;

public static class SafeLambdaLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void Log(
        ILambdaContext context,
        string eventType,
        string outcome,
        string? perfil = null,
        string? errorType = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["correlationId"] = context.AwsRequestId,
            ["eventType"] = eventType,
            ["outcome"] = outcome
        };

        if (!string.IsNullOrWhiteSpace(perfil))
            payload["perfil"] = perfil;

        if (!string.IsNullOrWhiteSpace(errorType))
            payload["errorType"] = errorType;

        context.Logger.LogLine(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
