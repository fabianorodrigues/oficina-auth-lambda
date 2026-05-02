using Microsoft.Extensions.DependencyInjection;
using Oficina.AuthLambda.Application.Abstractions;
using Oficina.AuthLambda.Application.Services;
using Oficina.AuthLambda.Configuration;
using Oficina.AuthLambda.Infrastructure.Database;
using Oficina.AuthLambda.Infrastructure.Security;
using Oficina.AuthLambda.Infrastructure.Time;

namespace Oficina.AuthLambda;

public static class DependencyInjection
{
    private static readonly Lazy<ServiceProvider> Provider = new(BuildServiceProvider);

    public static IServiceProvider ServiceProvider => Provider.Value;

    public static T GetRequiredService<T>() where T : notnull
        => ServiceProvider.GetRequiredService<T>();

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton(DatabaseOptions.FromEnvironment().Validate());
        services.AddSingleton(JwtOptions.FromEnvironment().Validate());
        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordVerifier, PasswordVerifier>();

        services.AddTransient<IClienteRepository, ClienteRepository>();
        services.AddTransient<IFuncionarioRepository, FuncionarioRepository>();
        services.AddTransient<AuthService>();
        services.AddTransient<JwtAuthorizerService>();

        return services.BuildServiceProvider(validateScopes: true);
    }
}
