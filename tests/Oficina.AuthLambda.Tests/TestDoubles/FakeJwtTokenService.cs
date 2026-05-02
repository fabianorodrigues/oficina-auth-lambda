using Oficina.AuthLambda.Application.Abstractions;
using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Contracts.Authorizer;
using Oficina.AuthLambda.Domain.Models;

namespace Oficina.AuthLambda.Tests.TestDoubles;

internal sealed class FakeJwtTokenService : IJwtTokenService
{
    public JwtValidationResult ValidationResult { get; set; } = JwtValidationResult.Invalid();

    public JwtTokenResult GenerateClienteToken(ClienteAuth cliente)
        => new("token-cliente", 3600);

    public JwtTokenResult GenerateFuncionarioToken(FuncionarioAuth funcionario)
        => new($"token-{funcionario.Perfil.ToString().ToLowerInvariant()}", 3600);

    public JwtValidationResult ValidateToken(string token) => ValidationResult;

    public static FakeJwtTokenService ValidCliente(string cpf, Guid clienteId)
        => new()
        {
            ValidationResult = JwtValidationResult.Valid(AuthorizerContext.Cliente(cpf, clienteId))
        };
}
