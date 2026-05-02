using Microsoft.AspNetCore.Identity;
using Oficina.AuthLambda.Infrastructure.Security;

namespace Oficina.AuthLambda.Tests.Infrastructure;

public class PasswordVerifierTests
{
    [Fact]
    public void Verify_ComSenhaCorreta_DeveRetornarTrue()
    {
        var hash = new PasswordHasher<object>().HashPassword(new object(), "Senha@123");
        var verifier = new PasswordVerifier();

        Assert.True(verifier.Verify(hash, "Senha@123"));
    }

    [Fact]
    public void Verify_ComSenhaIncorreta_DeveRetornarFalse()
    {
        var hash = new PasswordHasher<object>().HashPassword(new object(), "Senha@123");
        var verifier = new PasswordVerifier();

        Assert.False(verifier.Verify(hash, "errada"));
    }
}
