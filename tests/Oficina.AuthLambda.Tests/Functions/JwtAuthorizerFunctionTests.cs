using System.Text.Json;
using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Contracts.Authorizer;
using Oficina.AuthLambda.Functions;
using Oficina.AuthLambda.Tests.TestDoubles;
using Oficina.AuthLambda.Tests.TestSupport;

namespace Oficina.AuthLambda.Tests.Functions;

public class JwtAuthorizerFunctionTests
{
    [Fact]
    public void SemAuthorization_DeveNegar()
    {
        var function = new JwtAuthorizerFunction(new JwtAuthorizerService(new FakeJwtTokenService()));
        var context = new TestLambdaContext();

        var response = function.HandleAsync(new HttpApiAuthorizerRequest { Headers = [] }, context);

        Assert.False(response.IsAuthorized);
        AssertLog(context, "deny");
    }

    [Fact]
    public void AuthorizationMaiusculo_DeveAutorizar()
    {
        var clienteId = Guid.NewGuid();
        var function = new JwtAuthorizerFunction(new JwtAuthorizerService(
            FakeJwtTokenService.ValidCliente("39053344705", clienteId)));
        var context = new TestLambdaContext();

        var response = function.HandleAsync(
            new HttpApiAuthorizerRequest { Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" } },
            context);

        Assert.True(response.IsAuthorized);
        Assert.Equal("Cliente", response.Context!.Role);
        AssertLog(context, "allow");
    }

    [Fact]
    public void AuthorizationMinusculo_DeveAutorizar()
    {
        var clienteId = Guid.NewGuid();
        var function = new JwtAuthorizerFunction(new JwtAuthorizerService(
            FakeJwtTokenService.ValidCliente("39053344705", clienteId)));

        var response = function.HandleAsync(
            new HttpApiAuthorizerRequest { Headers = new Dictionary<string, string> { ["authorization"] = "Bearer token" } },
            new TestLambdaContext());

        Assert.True(response.IsAuthorized);
        Assert.Equal(clienteId.ToString(), response.Context!.ClienteId);
    }

    [Fact]
    public void AuthorizationComCasingMisto_DeveAutorizar()
    {
        var clienteId = Guid.NewGuid();
        var function = new JwtAuthorizerFunction(new JwtAuthorizerService(
            FakeJwtTokenService.ValidCliente("39053344705", clienteId)));

        var response = function.HandleAsync(
            new HttpApiAuthorizerRequest { Headers = new Dictionary<string, string> { ["aUtHoRiZaTiOn"] = "Bearer token" } },
            new TestLambdaContext());

        Assert.True(response.IsAuthorized);
    }

    private static void AssertLog(TestLambdaContext context, string outcome)
    {
        var logger = Assert.IsType<TestLambdaLogger>(context.Logger);
        var log = Assert.Single(logger.Lines);
        using var document = JsonDocument.Parse(log);

        Assert.Equal("request-id", document.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("JwtAuthorizer", document.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(outcome, document.RootElement.GetProperty("outcome").GetString());
        Assert.DoesNotContain("Bearer", log);
        Assert.DoesNotContain("token", log);
        Assert.DoesNotContain("39053344705", log);
    }
}
