using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Contracts.Auth;
using Oficina.AuthLambda.Domain.Models;
using Oficina.AuthLambda.Domain.ValueObjects;
using Oficina.AuthLambda.Functions;
using Oficina.AuthLambda.Tests.TestDoubles;
using Oficina.AuthLambda.Tests.TestSupport;

namespace Oficina.AuthLambda.Tests.Functions;

public class AuthCpfFunctionTests
{
    [Fact]
    public async Task EmptyBody_ShouldReturn400WithErrorContract()
    {
        var function = CreateFunction();

        var response = await function.HandleAsync(new APIGatewayHttpApiV2ProxyRequest { Body = "" }, new TestLambdaContext());

        AssertErrorResponse(response, 400);
    }

    [Fact]
    public async Task InvalidBase64Body_ShouldReturn400WithErrorContract()
    {
        var function = CreateFunction();

        var response = await function.HandleAsync(
            new APIGatewayHttpApiV2ProxyRequest { Body = "invalid-base64", IsBase64Encoded = true },
            new TestLambdaContext());

        AssertErrorResponse(response, 400);
    }

    [Fact]
    public async Task MalformedJson_ShouldReturn400WithErrorContract()
    {
        var function = CreateFunction();

        var response = await function.HandleAsync(
            new APIGatewayHttpApiV2ProxyRequest { Body = "{ \"cpf\": " },
            new TestLambdaContext());

        AssertErrorResponse(response, 400);
    }

    [Fact]
    public async Task ValidBase64Body_ShouldAuthenticateCliente()
    {
        var body = Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"cpf\":\"39053344705\"}"));
        var function = CreateFunction(new ClienteAuth(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Cpf.Parse("39053344705")));

        var response = await function.HandleAsync(
            new APIGatewayHttpApiV2ProxyRequest { Body = body, IsBase64Encoded = true },
            new TestLambdaContext());

        var auth = JsonSerializer.Deserialize<AuthCpfResponse>(response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal(200, response.StatusCode);
        Assert.Equal("token-cliente", auth!.AccessToken);
    }

    private static AuthCpfFunction CreateFunction(ClienteAuth? cliente = null)
    {
        var service = new AuthService(
            new FakeClienteRepository { Cliente = cliente },
            new FakeFuncionarioRepository(),
            new FakeJwtTokenService(),
            new FakePasswordVerifier());

        return new AuthCpfFunction(service);
    }

    private static void AssertErrorResponse(APIGatewayHttpApiV2ProxyResponse response, int expectedStatusCode)
    {
        Assert.Equal(expectedStatusCode, response.StatusCode);

        using var document = JsonDocument.Parse(response.Body);
        Assert.True(document.RootElement.TryGetProperty("erro", out var erro));
        Assert.False(string.IsNullOrWhiteSpace(erro.GetString()));
    }
}
