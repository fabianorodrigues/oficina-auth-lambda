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
    private static readonly Lazy<ServiceProvider> AuthProvider = new(BuildAuthServiceProvider);
    private static readonly Lazy<ServiceProvider> AuthorizerProvider = new(BuildAuthorizerServiceProvider);

    public static IServiceProvider AuthServiceProvider => AuthProvider.Value;

    public static IServiceProvider JwtAuthorizerServiceProvider => AuthorizerProvider.Value;

    public static T GetRequiredAuthService<T>() where T : notnull
        => AuthServiceProvider.GetRequiredService<T>();

    public static T GetRequiredAuthorizerService<T>() where T : notnull
        => JwtAuthorizerServiceProvider.GetRequiredService<T>();

    public static ServiceProvider BuildAuthServiceProvider()
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

        return services.BuildServiceProvider(validateScopes: true);
    }

    public static ServiceProvider BuildAuthorizerServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton(JwtOptions.FromEnvironment().Validate());
        services.AddSingleton<IClock, SystemClock>();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddTransient<JwtAuthorizerService>();

        return services.BuildServiceProvider(validateScopes: true);
    }
}
