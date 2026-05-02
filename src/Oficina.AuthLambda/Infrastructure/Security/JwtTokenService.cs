using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Oficina.AuthLambda.Application.Abstractions;
using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Configuration;
using Oficina.AuthLambda.Contracts.Authorizer;
using Oficina.AuthLambda.Domain.Enums;
using Oficina.AuthLambda.Domain.Models;

namespace Oficina.AuthLambda.Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;

    public JwtTokenService(JwtOptions options, IClock clock)
    {
        _options = options;
        _clock = clock;
    }

    public JwtTokenResult GenerateClienteToken(ClienteAuth cliente)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, cliente.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, PerfilAcesso.Cliente.ToString()),
            new("cpf", cliente.Cpf.Valor),
            new("clienteId", cliente.Id.ToString())
        };

        return GenerateToken(claims);
    }

    public JwtTokenResult GenerateFuncionarioToken(FuncionarioAuth funcionario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, funcionario.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, funcionario.Nome),
            new(ClaimTypes.Role, funcionario.Perfil.ToString()),
            new("cpf", funcionario.Cpf.Valor),
            new("funcionarioId", funcionario.Id.ToString())
        };

        return GenerateToken(claims);
    }

    public JwtValidationResult ValidateToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, BuildValidationParameters(), out var securityToken);

            if (securityToken is not JwtSecurityToken jwt ||
                !string.Equals(jwt.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
            {
                return JwtValidationResult.Invalid();
            }

            return TryBuildContext(principal, out var context)
                ? JwtValidationResult.Valid(context)
                : JwtValidationResult.Invalid();
        }
        catch
        {
            return JwtValidationResult.Invalid();
        }
    }

    private JwtTokenResult GenerateToken(IEnumerable<Claim> claims)
    {
        var now = _clock.UtcNow.UtcDateTime;
        var expires = now.AddMinutes(_options.ExpirationMinutes);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new JwtTokenResult(
            new JwtSecurityTokenHandler().WriteToken(token),
            _options.ExpirationMinutes * 60);
    }

    private TokenValidationParameters BuildValidationParameters()
        => new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role
        };

    private static bool TryBuildContext(ClaimsPrincipal principal, out AuthorizerContext context)
    {
        context = null!;

        var cpf = principal.FindFirst("cpf")?.Value;
        var role = principal.FindFirst(ClaimTypes.Role)?.Value ?? principal.FindFirst("role")?.Value;

        if (string.IsNullOrWhiteSpace(cpf) ||
            string.IsNullOrWhiteSpace(role) ||
            !Enum.TryParse<PerfilAcesso>(role, ignoreCase: true, out var perfil))
        {
            return false;
        }

        if (perfil is PerfilAcesso.Cliente)
        {
            var clienteIdValue = principal.FindFirst("clienteId")?.Value;
            if (!Guid.TryParse(clienteIdValue, out var clienteId))
            {
                return false;
            }

            context = AuthorizerContext.Cliente(cpf, clienteId);
            return true;
        }

        var funcionarioIdValue = principal.FindFirst("funcionarioId")?.Value;
        if (!Guid.TryParse(funcionarioIdValue, out var funcionarioId))
        {
            return false;
        }

        context = AuthorizerContext.Funcionario(cpf, perfil, funcionarioId);
        return true;
    }
}
