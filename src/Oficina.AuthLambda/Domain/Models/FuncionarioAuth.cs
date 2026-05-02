using Oficina.AuthLambda.Domain.Enums;
using Oficina.AuthLambda.Domain.ValueObjects;

namespace Oficina.AuthLambda.Domain.Models;

public sealed record FuncionarioAuth(
    Guid Id,
    string Nome,
    Cpf Cpf,
    string SenhaHash,
    PerfilAcesso Perfil,
    bool Ativo);
