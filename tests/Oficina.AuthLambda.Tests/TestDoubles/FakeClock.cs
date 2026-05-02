using Oficina.AuthLambda.Application.Abstractions;

namespace Oficina.AuthLambda.Tests.TestDoubles;

internal sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }
}
