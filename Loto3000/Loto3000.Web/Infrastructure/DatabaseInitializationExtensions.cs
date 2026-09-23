using Loto3000.DataAccess.Interfaces;
using Loto3000.Services.Interfaces;

namespace Loto3000.Web.Infrastructure;

/// <summary>Enforces environment-specific bootstrap safety at the composition boundary.</summary>
internal static class DatabaseInitializationExtensions
{
    /// <summary>Rejects known development secrets outside the Development environment.</summary>
    /// <param name="builder">The configured host builder.</param>
    /// <exception cref="InvalidOperationException">A production deployment uses missing/default bootstrap secrets.</exception>
    public static void ValidateProductionSecrets(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            return;
        }

        var key = builder.Configuration["Jwt:SigningKey"];
        var password = builder.Configuration["BootstrapAdmin:Password"];
        if (key?.StartsWith("DevelopmentOnly-", StringComparison.Ordinal) == true)
        {
            throw new InvalidOperationException("Development JWT signing keys are forbidden outside Development.");
        }

        if (string.IsNullOrWhiteSpace(password) || password == "Admin@123")
        {
            throw new InvalidOperationException("Supply a strong, non-default BootstrapAdmin:Password through a secret store or environment variable outside Development.");
        }
    }

    /// <summary>Runs configured migrations and idempotent seeding, then verifies production bootstrap identity.</summary>
    /// <param name="app">The built application whose scoped services initialize persistence.</param>
    /// <returns>A task completing before the server starts accepting requests.</returns>
    /// <exception cref="InvalidOperationException">Required bootstrap configuration is absent.</exception>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        var password = app.Configuration["BootstrapAdmin:Password"]
            ?? throw new InvalidOperationException("BootstrapAdmin:Password is required.");
        var applyMigrations = bool.TryParse(app.Configuration["Database:ApplyMigrationsOnStartup"], out var enabled) && enabled;
        await using var scope = app.Services.CreateAsyncScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
        await seeder.SeedAsync(authService.HashPassword(password), applyMigrations, app.Lifetime.ApplicationStopping);
        if (!app.Environment.IsDevelopment())
        {
            await authService.ValidateBootstrapAdministratorAsync(password, app.Lifetime.ApplicationStopping);
        }
    }
}
