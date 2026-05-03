using Microsoft.Extensions.DependencyInjection;
using Oficina.AuthLambda.Application.Abstractions;
using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Configuration;

namespace Oficina.AuthLambda.Tests;

public sealed class DependencyInjectionTests
{
    private const string ValidConnectionString =
        "Server=localhost,1433;Database=OficinaDb;User Id=sa;Password=Senha@123;Encrypt=True;TrustServerCertificate=True";

    private const string ValidJwtSecret = "12345678901234567890123456789012";

    [Fact]
    public void AuthorizerProvider_SemConnectionString_DeveResolverAuthorizer()
    {
        using var env = new EnvironmentScope(
            jwtSecret: ValidJwtSecret,
            jwtIssuer: "oficina-auth",
            jwtAudience: "oficina-api",
            jwtExpirationMinutes: "120");

        using var provider = DependencyInjection.BuildAuthorizerServiceProvider();

        Assert.NotNull(provider.GetRequiredService<JwtAuthorizerService>());
    }

    [Fact]
    public void AuthProvider_SemConnectionString_DeveFalhar()
    {
        using var env = new EnvironmentScope(
            jwtSecret: ValidJwtSecret,
            jwtIssuer: "oficina-auth",
            jwtAudience: "oficina-api",
            jwtExpirationMinutes: "120");

        var ex = Assert.Throws<InvalidOperationException>(DependencyInjection.BuildAuthServiceProvider);

        Assert.Contains("ConnectionStrings__SqlServer", ex.Message);
    }

    [Fact]
    public void AuthorizerProvider_SemJwt_DeveFalhar()
    {
        using var env = new EnvironmentScope();

        var ex = Assert.Throws<InvalidOperationException>(DependencyInjection.BuildAuthorizerServiceProvider);

        Assert.Contains("Jwt__Secret", ex.Message);
    }

    [Fact]
    public void AuthProvider_ComDbEJwt_DeveResolverAuthService()
    {
        using var env = new EnvironmentScope(
            connectionString: ValidConnectionString,
            jwtSecret: ValidJwtSecret,
            jwtIssuer: "oficina-auth",
            jwtAudience: "oficina-api",
            jwtExpirationMinutes: "120");

        using var provider = DependencyInjection.BuildAuthServiceProvider();

        Assert.NotNull(provider.GetRequiredService<AuthService>());
    }

    [Fact]
    public void AuthorizerProvider_NaoDeveRegistrarDependenciasSql()
    {
        using var env = new EnvironmentScope(
            jwtSecret: ValidJwtSecret,
            jwtIssuer: "oficina-auth",
            jwtAudience: "oficina-api",
            jwtExpirationMinutes: "120");

        using var provider = DependencyInjection.BuildAuthorizerServiceProvider();

        Assert.Null(provider.GetService<DatabaseOptions>());
        Assert.Null(provider.GetService<ISqlConnectionFactory>());
        Assert.Null(provider.GetService<IClienteRepository>());
        Assert.Null(provider.GetService<IFuncionarioRepository>());
        Assert.Null(provider.GetService<IPasswordVerifier>());
    }

    private sealed class EnvironmentScope : IDisposable
    {
        private readonly Dictionary<string, string?> _previousValues;

        public EnvironmentScope(
            string? connectionString = null,
            string? jwtSecret = null,
            string? jwtIssuer = null,
            string? jwtAudience = null,
            string? jwtExpirationMinutes = null)
        {
            _previousValues = new Dictionary<string, string?>
            {
                ["ConnectionStrings__SqlServer"] = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer"),
                ["ConnectionStrings:SqlServer"] = Environment.GetEnvironmentVariable("ConnectionStrings:SqlServer"),
                ["Jwt__Secret"] = Environment.GetEnvironmentVariable("Jwt__Secret"),
                ["Jwt:Secret"] = Environment.GetEnvironmentVariable("Jwt:Secret"),
                ["Jwt__Key"] = Environment.GetEnvironmentVariable("Jwt__Key"),
                ["Jwt:Key"] = Environment.GetEnvironmentVariable("Jwt:Key"),
                ["Jwt__Issuer"] = Environment.GetEnvironmentVariable("Jwt__Issuer"),
                ["Jwt:Issuer"] = Environment.GetEnvironmentVariable("Jwt:Issuer"),
                ["Jwt__Audience"] = Environment.GetEnvironmentVariable("Jwt__Audience"),
                ["Jwt:Audience"] = Environment.GetEnvironmentVariable("Jwt:Audience"),
                ["Jwt__ExpirationMinutes"] = Environment.GetEnvironmentVariable("Jwt__ExpirationMinutes"),
                ["Jwt:ExpirationMinutes"] = Environment.GetEnvironmentVariable("Jwt:ExpirationMinutes"),
                ["Jwt__ExpMinutes"] = Environment.GetEnvironmentVariable("Jwt__ExpMinutes"),
                ["Jwt:ExpMinutes"] = Environment.GetEnvironmentVariable("Jwt:ExpMinutes")
            };

            foreach (var name in _previousValues.Keys)
            {
                Environment.SetEnvironmentVariable(name, null);
            }

            Environment.SetEnvironmentVariable("ConnectionStrings__SqlServer", connectionString);
            Environment.SetEnvironmentVariable("Jwt__Secret", jwtSecret);
            Environment.SetEnvironmentVariable("Jwt__Issuer", jwtIssuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", jwtAudience);
            Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", jwtExpirationMinutes);
        }

        public void Dispose()
        {
            foreach (var item in _previousValues)
            {
                Environment.SetEnvironmentVariable(item.Key, item.Value);
            }
        }
    }
}
