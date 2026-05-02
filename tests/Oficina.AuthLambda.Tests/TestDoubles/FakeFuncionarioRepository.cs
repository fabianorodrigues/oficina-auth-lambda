using Oficina.AuthLambda.Application.Abstractions;
using Oficina.AuthLambda.Domain.Models;
using Oficina.AuthLambda.Domain.ValueObjects;

namespace Oficina.AuthLambda.Tests.TestDoubles;

internal sealed class FakeFuncionarioRepository : IFuncionarioRepository
{
    public FuncionarioAuth? Funcionario { get; set; }

    public Task<FuncionarioAuth?> ObterPorCpfAsync(Cpf cpf, CancellationToken cancellationToken)
        => Task.FromResult(Funcionario);
}
