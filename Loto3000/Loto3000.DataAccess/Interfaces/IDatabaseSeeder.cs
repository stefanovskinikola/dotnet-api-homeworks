namespace Loto3000.DataAccess.Interfaces;

/// <summary>Initializes an empty database without resetting existing accounts or sessions.</summary>
public interface IDatabaseSeeder
{
    /// <summary>Optionally migrates the schema and atomically seeds the first administrator and session.</summary>
    /// <param name="adminPasswordHash">A salted hash created by Services; never plaintext.</param>
    /// <param name="applyMigrations">Whether startup may apply pending migrations.</param>
    /// <param name="cancellationToken">Cancels database operations.</param>
    /// <returns>A task completing after bootstrap changes are committed.</returns>
    /// <exception cref="InvalidOperationException">Migrations are pending but automatic migration is disabled.</exception>
    Task SeedAsync(string adminPasswordHash, bool applyMigrations = false, CancellationToken cancellationToken = default);
}
