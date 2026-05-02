using Oficina.AuthLambda.Application.Abstractions;
using Oficina.AuthLambda.Domain.Enums;
using Oficina.AuthLambda.Domain.Exceptions;
using Oficina.AuthLambda.Domain.ValueObjects;
using Oficina.AuthLambda.Contracts.Auth;

namespace Oficina.AuthLambda.Application.Services;

public sealed class AuthService
{
    private readonly IClienteRepository _clientes;
    private readonly IFuncionarioRepository _funcionarios;
    private readonly IJwtTokenService _jwt;
    private readonly IPasswordVerifier _passwordVerifier;

    public AuthService(
        IClienteRepository clientes,
        IFuncionarioRepository funcionarios,
        IJwtTokenService jwt,
        IPasswordVerifier passwordVerifier)
    {
        _clientes = clientes;
        _funcionarios = funcionarios;
        _jwt = jwt;
        _passwordVerifier = passwordVerifier;
    }

    public async Task<AuthResult> AuthenticateAsync(AuthCpfRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Cpf))
        {
            throw AuthException.BadRequest("CPF is required.");
        }

        var cpf = Cpf.Parse(request.Cpf);

        if (string.IsNullOrWhiteSpace(request.Senha))
        {
            return await AuthenticateClienteAsync(cpf, cancellationToken);
        }

        return await AuthenticateFuncionarioAsync(cpf, request.Senha, cancellationToken);
    }

    private async Task<AuthResult> AuthenticateClienteAsync(Cpf cpf, CancellationToken cancellationToken)
    {
        var cliente = await _clientes.ObterPorCpfAsync(cpf, cancellationToken);

        if (cliente is null || !cliente.Ativo)
        {
            throw AuthException.Unauthorized();
        }

        var token = _jwt.GenerateClienteToken(cliente);

        return new AuthResult(
            token.AccessToken,
            token.ExpiresIn,
            PerfilAcesso.Cliente,
            cliente.Id,
            FuncionarioId: null);
    }

    private async Task<AuthResult> AuthenticateFuncionarioAsync(
        Cpf cpf,
        string senha,
        CancellationToken cancellationToken)
    {
        var funcionario = await _funcionarios.ObterPorCpfAsync(cpf, cancellationToken);

        if (funcionario is null ||
            !funcionario.Ativo ||
            !_passwordVerifier.Verify(funcionario.SenhaHash, senha))
        {
            throw AuthException.Unauthorized();
        }

        var token = _jwt.GenerateFuncionarioToken(funcionario);

        return new AuthResult(
            token.AccessToken,
            token.ExpiresIn,
            funcionario.Perfil,
            ClienteId: null,
            funcionario.Id);
    }
}
