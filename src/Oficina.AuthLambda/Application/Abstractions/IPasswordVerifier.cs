namespace Oficina.AuthLambda.Application.Abstractions;

public interface IPasswordVerifier
{
    bool Verify(string senhaHash, string senha);
}
