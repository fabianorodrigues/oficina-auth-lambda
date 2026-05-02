using Oficina.AuthLambda.Domain.Exceptions;
using Oficina.AuthLambda.Domain.ValueObjects;

namespace Oficina.AuthLambda.Tests.Domain;

public class CpfTests
{
    [Fact]
    public void Parse_ComCpfValido_DeveCriarValueObject()
    {
        var cpf = Cpf.Parse("39053344705");

        Assert.Equal("39053344705", cpf.Valor);
    }

    [Fact]
    public void Parse_ComMascara_DeveNormalizar()
    {
        var cpf = Cpf.Parse("390.533.447-05");

        Assert.Equal("39053344705", cpf.Valor);
    }

    [Fact]
    public void Parse_ComCpfInvalido_DeveFalhar()
        => Assert.Throws<AuthException>(() => Cpf.Parse("12345678900"));

    [Fact]
    public void Parse_ComTodosDigitosIguais_DeveFalhar()
        => Assert.Throws<AuthException>(() => Cpf.Parse("11111111111"));
}
