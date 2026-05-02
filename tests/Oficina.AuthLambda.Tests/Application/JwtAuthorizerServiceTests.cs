using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Tests.TestDoubles;

namespace Oficina.AuthLambda.Tests.Application;

public class JwtAuthorizerServiceTests
{
    [Fact]
    public void SemAuthorization_DeveNegar()
    {
        var service = new JwtAuthorizerService(new FakeJwtTokenService());

        var response = service.Authorize(null);

        Assert.False(response.IsAuthorized);
    }

    [Fact]
    public void AuthorizationSemBearer_DeveNegar()
    {
        var service = new JwtAuthorizerService(new FakeJwtTokenService());

        var response = service.Authorize("token");

        Assert.False(response.IsAuthorized);
    }

    [Fact]
    public void TokenInvalido_DeveNegar()
    {
        var service = new JwtAuthorizerService(new FakeJwtTokenService());

        var response = service.Authorize("Bearer token");

        Assert.False(response.IsAuthorized);
    }

    [Fact]
    public void TokenValido_DeveAutorizar()
    {
        var clienteId = Guid.NewGuid();
        var service = new JwtAuthorizerService(FakeJwtTokenService.ValidCliente("39053344705", clienteId));

        var response = service.Authorize("Bearer token-valido");

        Assert.True(response.IsAuthorized);
        Assert.Equal("39053344705", response.Context!.Cpf);
        Assert.Equal(clienteId.ToString(), response.Context.ClienteId);
    }
}
