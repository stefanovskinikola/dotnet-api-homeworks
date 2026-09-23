using Loto3000.Services.Interfaces;
using Loto3000.Services.DTOs.Auth;
using Loto3000.Services.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Loto3000.Services;

/// <summary>Registers business services and validated authentication configuration.</summary>
public static class DependencyInjection
{
    /// <summary>Adds scoped service implementations without resolving persistence directly.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="configuration">The configuration supplying JWT settings.</param>
    /// <returns>The same collection for chained registrations.</returns>
    /// <exception cref="InvalidOperationException">JWT configuration is invalid.</exception>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(JwtOptions.FromConfiguration(configuration));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IDrawService, DrawService>();
        services.AddScoped<IWinnerService, WinnerService>();
        return services;
    }
}
