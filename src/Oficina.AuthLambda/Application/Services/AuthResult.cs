using Oficina.AuthLambda.Domain.Enums;

namespace Oficina.AuthLambda.Application.Services;

public sealed record AuthResult(
    string AccessToken,
    int ExpiresIn,
    PerfilAcesso Perfil,
    Guid? ClienteId,
    Guid? FuncionarioId);
