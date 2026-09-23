using Loto3000.DataAccess.Interfaces;
using Loto3000.DataAccess.Implementations;
using Loto3000.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Loto3000.DataAccess;

/// <summary>Registers scoped SQL Server persistence without introducing dependencies into Domain.</summary>
public static class DependencyInjection
{
    /// <summary>Registers the context, repositories, unit of work and bootstrap seeder.</summary>
    /// <param name="services">The application's service collection.</param>
    /// <param name="connectionString">The SQL Server connection string.</param>
    /// <returns>The same collection for chained registrations.</returns>
    /// <exception cref="ArgumentException">The connection string is blank.</exception>
    public static IServiceCollection AddDataAccessLayer(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        // Keep EF's default non-retrying SQL execution strategy. Retrying individual commands
        // cannot safely replay a multi-save lottery transaction or resolve an ambiguous commit.
        services.AddDbContext<Loto3000DbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.CommandTimeout(30)));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<IDrawRepository, DrawRepository>();
        services.AddScoped<IWinnerRepository, WinnerRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();
        return services;
    }
}
