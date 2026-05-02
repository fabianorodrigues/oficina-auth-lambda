using Oficina.AuthLambda.Domain.Exceptions;

namespace Oficina.AuthLambda.Domain.ValueObjects;

public sealed record Cpf
{
    private Cpf(string valor)
    {
        Valor = valor;
    }

    public string Valor { get; }

    public static Cpf Parse(string? value)
    {
        var normalized = Normalize(value);

        if (!IsValid(normalized))
        {
            throw AuthException.BadRequest("Invalid CPF.");
        }

        return new Cpf(normalized);
    }

    public static bool TryParse(string? value, out Cpf? cpf)
    {
        var normalized = Normalize(value);
        if (!IsValid(normalized))
        {
            cpf = null;
            return false;
        }

        cpf = new Cpf(normalized);
        return true;
    }

    public override string ToString() => Valor;

    private static string Normalize(string? value)
        => value is null
            ? string.Empty
            : new string(value.Where(char.IsDigit).ToArray());

    private static bool IsValid(string cpf)
    {
        if (cpf.Length != 11 || cpf.All(c => c == cpf[0]))
        {
            return false;
        }

        var firstDigit = CalculateDigit(cpf, 9, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        var secondDigit = CalculateDigit(cpf, 10, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);

        return cpf[9] - '0' == firstDigit && cpf[10] - '0' == secondDigit;
    }

    private static int CalculateDigit(string cpf, int baseLength, IReadOnlyList<int> weights)
    {
        var sum = 0;
        for (var i = 0; i < baseLength; i++)
        {
            sum += (cpf[i] - '0') * weights[i];
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
