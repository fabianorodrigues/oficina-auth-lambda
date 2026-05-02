namespace Oficina.AuthLambda.Configuration;

public sealed class DatabaseOptions
{
    public required string ConnectionString { get; init; }

    public static DatabaseOptions FromEnvironment()
        => new()
        {
            ConnectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer") ??
                Environment.GetEnvironmentVariable("ConnectionStrings:SqlServer") ??
                string.Empty
        };

    public DatabaseOptions Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
        {
            throw new InvalidOperationException("ConnectionStrings__SqlServer missing.");
        }

        return this;
    }
}
