using Oficina.AuthLambda.Domain.Models;
using Oficina.AuthLambda.Domain.ValueObjects;

namespace Oficina.AuthLambda.Application.Abstractions;

public interface IClienteRepository
{
    Task<ClienteAuth?> ObterPorCpfAsync(Cpf cpf, CancellationToken cancellationToken);
}
