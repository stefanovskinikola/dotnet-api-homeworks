using Loto3000.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Loto3000.Web.Infrastructure;

/// <summary>Creates a migrations-only context without bootstrapping the web host or requiring JWT secrets.</summary>
public sealed class Loto3000DbContextFactory : IDesignTimeDbContextFactory<Loto3000DbContext>
{
    /// <summary>Reads SQL configuration from the Web project and environment for EF tooling.</summary>
    /// <param name="args">Optional configuration overrides passed through the EF CLI.</param>
    /// <returns>A SQL Server context; no seed data or migrations are applied by this factory.</returns>
    /// <exception cref="InvalidOperationException">The connection string is not configured.</exception>
    public Loto3000DbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var webDirectory = Path.Combine(currentDirectory, "Loto3000.Web");
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(webDirectory) ? webDirectory : currentDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        var options = new DbContextOptionsBuilder<Loto3000DbContext>().UseSqlServer(connectionString).Options;
        return new Loto3000DbContext(options);
    }
}
