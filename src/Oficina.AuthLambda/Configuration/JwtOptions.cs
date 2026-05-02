namespace Oficina.AuthLambda.Configuration;

public sealed class JwtOptions
{
    public required string Secret { get; init; }
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public int ExpirationMinutes { get; init; } = 120;

    public static JwtOptions FromEnvironment()
    {
        var expirationValue =
            Environment.GetEnvironmentVariable("Jwt__ExpirationMinutes") ??
            Environment.GetEnvironmentVariable("Jwt:ExpirationMinutes") ??
            Environment.GetEnvironmentVariable("Jwt__ExpMinutes") ??
            Environment.GetEnvironmentVariable("Jwt:ExpMinutes");

        return new JwtOptions
        {
            Secret =
                Environment.GetEnvironmentVariable("Jwt__Secret") ??
                Environment.GetEnvironmentVariable("Jwt:Secret") ??
                Environment.GetEnvironmentVariable("Jwt__Key") ??
                Environment.GetEnvironmentVariable("Jwt:Key") ??
                string.Empty,
            Issuer =
                Environment.GetEnvironmentVariable("Jwt__Issuer") ??
                Environment.GetEnvironmentVariable("Jwt:Issuer") ??
                string.Empty,
            Audience =
                Environment.GetEnvironmentVariable("Jwt__Audience") ??
                Environment.GetEnvironmentVariable("Jwt:Audience") ??
                string.Empty,
            ExpirationMinutes = int.TryParse(expirationValue, out var minutes) ? minutes : 120
        };
    }

    public JwtOptions Validate()
    {
        if (string.IsNullOrWhiteSpace(Secret) || Secret.Length < 32)
        {
            throw new InvalidOperationException("Jwt__Secret must have at least 32 characters.");
        }

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            throw new InvalidOperationException("Jwt__Issuer missing.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt__Audience missing.");
        }

        if (ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt__ExpirationMinutes must be greater than zero.");
        }

        return this;
    }
}
