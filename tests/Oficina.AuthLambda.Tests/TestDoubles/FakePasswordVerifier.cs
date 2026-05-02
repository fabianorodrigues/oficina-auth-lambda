using Oficina.AuthLambda.Application.Abstractions;

namespace Oficina.AuthLambda.Tests.TestDoubles;

internal sealed class FakePasswordVerifier : IPasswordVerifier
{
    public bool IsValid { get; set; }

    public bool Verify(string senhaHash, string senha) => IsValid;
}
