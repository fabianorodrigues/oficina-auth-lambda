using Oficina.AuthLambda.Domain.Models;
using Oficina.AuthLambda.Application.Services;

namespace Oficina.AuthLambda.Application.Abstractions;

public interface IJwtTokenService
{
    JwtTokenResult GenerateClienteToken(ClienteAuth cliente);
    JwtTokenResult GenerateFuncionarioToken(FuncionarioAuth funcionario);
    JwtValidationResult ValidateToken(string token);
}
