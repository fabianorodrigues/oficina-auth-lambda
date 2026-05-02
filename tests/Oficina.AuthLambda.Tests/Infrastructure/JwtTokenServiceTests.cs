using Oficina.AuthLambda.Configuration;
using Oficina.AuthLambda.Domain.Enums;
using Oficina.AuthLambda.Domain.Models;
using Oficina.AuthLambda.Domain.ValueObjects;
using Oficina.AuthLambda.Infrastructure.Security;
using Oficina.AuthLambda.Tests.TestDoubles;

namespace Oficina.AuthLambda.Tests.Infrastructure;

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateClienteToken_DeveGerarRoleCliente()
    {
        var service = CreateService();
        var cliente = new ClienteAuth(Guid.NewGuid(), Cpf.Parse("39053344705"));

        var token = service.GenerateClienteToken(cliente);
        var validation = service.ValidateToken(token.AccessToken);

        Assert.True(validation.IsValid);
        Assert.Equal(3600, token.ExpiresIn);
        Assert.Equal(PerfilAcesso.Cliente.ToString(), validation.Context!.Role);
        Assert.Equal(cliente.Id.ToString(), validation.Context.ClienteId);
        Assert.Equal(cliente.Cpf.Valor, validation.Context.Cpf);
    }

    [Theory]
    [InlineData(PerfilAcesso.Funcionario)]
    [InlineData(PerfilAcesso.Admin)]
    public void GenerateFuncionarioToken_DeveGerarPerfilInterno(PerfilAcesso perfil)
    {
        var service = CreateService();
        var funcionario = new FuncionarioAuth(
            Guid.NewGuid(),
            "Maria",
            Cpf.Parse("39053344705"),
            "hash",
            perfil,
            true);

        var token = service.GenerateFuncionarioToken(funcionario);
        var validation = service.ValidateToken(token.AccessToken);

        Assert.True(validation.IsValid);
        Assert.Equal(perfil.ToString(), validation.Context!.Role);
        Assert.Equal(funcionario.Id.ToString(), validation.Context.FuncionarioId);
    }

    [Fact]
    public void ValidateToken_ComIssuerInvalido_DeveRejeitar()
    {
        var token = CreateService().GenerateClienteToken(new ClienteAuth(Guid.NewGuid(), Cpf.Parse("39053344705")));
        var wrongIssuer = CreateService(new JwtOptions
        {
            Secret = Secret,
            Issuer = "Outro.Issuer",
            Audience = "Oficina.Api",
            ExpirationMinutes = 60
        });

        Assert.False(wrongIssuer.ValidateToken(token.AccessToken).IsValid);
    }

    [Fact]
    public void ValidateToken_ComAudienceInvalida_DeveRejeitar()
    {
        var token = CreateService().GenerateClienteToken(new ClienteAuth(Guid.NewGuid(), Cpf.Parse("39053344705")));
        var wrongAudience = CreateService(new JwtOptions
        {
            Secret = Secret,
            Issuer = "Oficina.Api",
            Audience = "Outra.Api",
            ExpirationMinutes = 60
        });

        Assert.False(wrongAudience.ValidateToken(token.AccessToken).IsValid);
    }

    [Fact]
    public void ValidateToken_Expirado_DeveRejeitar()
    {
        var expiredGenerator = CreateService(clock: new FakeClock(DateTimeOffset.UtcNow.AddMinutes(-120)));
        var token = expiredGenerator.GenerateClienteToken(new ClienteAuth(Guid.NewGuid(), Cpf.Parse("39053344705")));

        Assert.False(CreateService().ValidateToken(token.AccessToken).IsValid);
    }

    [Fact]
    public void ValidateToken_ComSecretErrado_DeveRejeitar()
    {
        var token = CreateService().GenerateClienteToken(new ClienteAuth(Guid.NewGuid(), Cpf.Parse("39053344705")));
        var wrongSecret = CreateService(new JwtOptions
        {
            Secret = "outra-chave-super-secreta-com-mais-de-32-caracteres",
            Issuer = "Oficina.Api",
            Audience = "Oficina.Api",
            ExpirationMinutes = 60
        });

        Assert.False(wrongSecret.ValidateToken(token.AccessToken).IsValid);
    }

    private const string Secret = "oficina-test-secret-key-with-32-characters";

    private static JwtTokenService CreateService(JwtOptions? options = null, FakeClock? clock = null)
        => new((options ?? new JwtOptions
        {
            Secret = Secret,
            Issuer = "Oficina.Api",
            Audience = "Oficina.Api",
            ExpirationMinutes = 60
        }).Validate(), clock ?? new FakeClock(DateTimeOffset.UtcNow));
}
