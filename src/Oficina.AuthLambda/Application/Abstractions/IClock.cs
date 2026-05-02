namespace Oficina.AuthLambda.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
