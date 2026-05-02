using Oficina.AuthLambda.Domain.ValueObjects;

namespace Oficina.AuthLambda.Domain.Models;

public sealed record ClienteAuth(Guid Id, Cpf Cpf, bool Ativo = true);
