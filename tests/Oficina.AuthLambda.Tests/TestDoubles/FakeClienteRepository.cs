using Oficina.AuthLambda.Application.Abstractions;
using Oficina.AuthLambda.Domain.Models;
using Oficina.AuthLambda.Domain.ValueObjects;

namespace Oficina.AuthLambda.Tests.TestDoubles;

internal sealed class FakeClienteRepository : IClienteRepository
{
    public ClienteAuth? Cliente { get; set; }

    public Task<ClienteAuth?> ObterPorCpfAsync(Cpf cpf, CancellationToken cancellationToken)
        => Task.FromResult(Cliente);
}
