using Microsoft.AspNetCore.Identity;
using Oficina.AuthLambda.Application.Abstractions;

namespace Oficina.AuthLambda.Infrastructure.Security;

public sealed class PasswordVerifier : IPasswordVerifier
{
    private readonly PasswordHasher<object> _hasher = new();

    public bool Verify(string senhaHash, string senha)
    {
        if (string.IsNullOrWhiteSpace(senhaHash) || string.IsNullOrWhiteSpace(senha))
        {
            return false;
        }

        var result = _hasher.VerifyHashedPassword(new object(), senhaHash, senha);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
