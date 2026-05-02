using Oficina.AuthLambda.Domain.Models;
using Oficina.AuthLambda.Domain.ValueObjects;

namespace Oficina.AuthLambda.Application.Abstractions;

public interface IFuncionarioRepository
{
    Task<FuncionarioAuth?> ObterPorCpfAsync(Cpf cpf, CancellationToken cancellationToken);
}
