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

        var response = function.HandleAsync(new HttpApiAuthorizerRequest { Headers = [] }, new TestLambdaContext());

        Assert.False(response.IsAuthorized);
    }

    [Fact]
    public void AuthorizationMaiusculo_DeveAutorizar()
    {
        var clienteId = Guid.NewGuid();
        var function = new JwtAuthorizerFunction(new JwtAuthorizerService(
            FakeJwtTokenService.ValidCliente("39053344705", clienteId)));

        var response = function.HandleAsync(
            new HttpApiAuthorizerRequest { Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" } },
            new TestLambdaContext());

        Assert.True(response.IsAuthorized);
        Assert.Equal("Cliente", response.Context!.Role);
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
}
