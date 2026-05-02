using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Contracts.Auth;
using Oficina.AuthLambda.Domain.Enums;
using Oficina.AuthLambda.Domain.Exceptions;
using Oficina.AuthLambda.Domain.Models;
using Oficina.AuthLambda.Domain.ValueObjects;
using Oficina.AuthLambda.Tests.TestDoubles;

namespace Oficina.AuthLambda.Tests.Application;

public class AuthServiceTests
{
    [Fact]
    public async Task ClienteValido_DeveRetornarToken()
    {
        var clienteId = Guid.NewGuid();
        var fixture = CreateFixture();
        fixture.Clientes.Cliente = new ClienteAuth(clienteId, Cpf.Parse("39053344705"));

        var response = await fixture.Service.AuthenticateAsync(new AuthCpfRequest { Cpf = "39053344705" }, CancellationToken.None);

        Assert.Equal("token-cliente", response.AccessToken);
        Assert.Equal(PerfilAcesso.Cliente, response.Perfil);
        Assert.Equal(clienteId, response.ClienteId);
        Assert.Null(response.FuncionarioId);
    }

    [Fact]
    public async Task ClienteInexistente_DeveRetornar401()
    {
        var fixture = CreateFixture();

        var ex = await Assert.ThrowsAsync<AuthException>(() =>
            fixture.Service.AuthenticateAsync(new AuthCpfRequest { Cpf = "39053344705" }, CancellationToken.None));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task ClienteInativo_DeveRetornar401()
    {
        var fixture = CreateFixture();
        fixture.Clientes.Cliente = new ClienteAuth(Guid.NewGuid(), Cpf.Parse("39053344705"), Ativo: false);

        var ex = await Assert.ThrowsAsync<AuthException>(() =>
            fixture.Service.AuthenticateAsync(new AuthCpfRequest { Cpf = "39053344705" }, CancellationToken.None));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task FuncionarioValido_DeveRetornarToken()
    {
        var funcionarioId = Guid.NewGuid();
        var fixture = CreateFixture();
        fixture.Funcionarios.Funcionario = new FuncionarioAuth(
            funcionarioId,
            "Funcionario",
            Cpf.Parse("39053344705"),
            "hash",
            PerfilAcesso.Funcionario,
            true);
        fixture.Password.IsValid = true;

        var response = await fixture.Service.AuthenticateAsync(
            new AuthCpfRequest { Cpf = "39053344705", Senha = "Senha@123" },
            CancellationToken.None);

        Assert.Equal("token-funcionario", response.AccessToken);
        Assert.Equal(PerfilAcesso.Funcionario, response.Perfil);
        Assert.Null(response.ClienteId);
        Assert.Equal(funcionarioId, response.FuncionarioId);
    }

    [Fact]
    public async Task FuncionarioComSenhaErrada_DeveRetornar401()
    {
        var fixture = CreateFixture();
        fixture.Funcionarios.Funcionario = new FuncionarioAuth(
            Guid.NewGuid(),
            "Funcionario",
            Cpf.Parse("39053344705"),
            "hash",
            PerfilAcesso.Funcionario,
            true);
        fixture.Password.IsValid = false;

        var ex = await Assert.ThrowsAsync<AuthException>(() =>
            fixture.Service.AuthenticateAsync(
                new AuthCpfRequest { Cpf = "39053344705", Senha = "errada" },
                CancellationToken.None));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task FuncionarioInativo_DeveRetornar401()
    {
        var fixture = CreateFixture();
        fixture.Funcionarios.Funcionario = new FuncionarioAuth(
            Guid.NewGuid(),
            "Funcionario",
            Cpf.Parse("39053344705"),
            "hash",
            PerfilAcesso.Funcionario,
            false);
        fixture.Password.IsValid = true;

        var ex = await Assert.ThrowsAsync<AuthException>(() =>
            fixture.Service.AuthenticateAsync(
                new AuthCpfRequest { Cpf = "39053344705", Senha = "Senha@123" },
                CancellationToken.None));

        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public async Task RequestSemCpf_DeveRetornar400()
    {
        var fixture = CreateFixture();

        var ex = await Assert.ThrowsAsync<AuthException>(() =>
            fixture.Service.AuthenticateAsync(new AuthCpfRequest { Cpf = "" }, CancellationToken.None));

        Assert.Equal(400, ex.StatusCode);
    }

    private static AuthFixture CreateFixture()
    {
        var clientes = new FakeClienteRepository();
        var funcionarios = new FakeFuncionarioRepository();
        var password = new FakePasswordVerifier();
        var service = new AuthService(clientes, funcionarios, new FakeJwtTokenService(), password);

        return new AuthFixture(service, clientes, funcionarios, password);
    }

    private sealed record AuthFixture(
        AuthService Service,
        FakeClienteRepository Clientes,
        FakeFuncionarioRepository Funcionarios,
        FakePasswordVerifier Password);
}
