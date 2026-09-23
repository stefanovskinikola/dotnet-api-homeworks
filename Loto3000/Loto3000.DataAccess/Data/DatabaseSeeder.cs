using System.Data;
using Loto3000.DataAccess.Interfaces;
using Loto3000.Domain.Models;
using Loto3000.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Loto3000.DataAccess.Data;

/// <summary>Serializes first-run bootstrap across application instances without resetting existing data.</summary>
internal sealed class DatabaseSeeder(Loto3000DbContext context, ILogger<DatabaseSeeder> logger) : IDatabaseSeeder
{
    /// <inheritdoc />
    public async Task SeedAsync(string adminPasswordHash, bool applyMigrations = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminPasswordHash);

        if (applyMigrations)
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        else if ((await context.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
        {
            throw new InvalidOperationException("Database migrations are pending. Run dotnet ef database update --project Loto3000.DataAccess --startup-project Loto3000.Web before starting the application.");
        }

        // The transaction-owned application lock prevents competing instances from seeding twice.
        // Migrations run before this transaction; account/session creation is one atomic bootstrap.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await context.Database.ExecuteSqlRawAsync(
            "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource = N'Loto3000.Seed', @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000; IF @result < 0 THROW 51000, 'Could not acquire the database seed lock.', 1;",
            cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (!await context.Users.AnyAsync(cancellationToken))
        {
            context.Users.Add(new User
            {
                Username = "admin",
                FirstName = "Loto3000",
                LastName = "Administrator",
                Email = "admin@loto3000.local",
                Role = UserRole.Admin,
                PasswordHash = adminPasswordHash,
                CreatedAt = now
            });
            logger.LogInformation("Created the initial administrator account.");
        }

        if (!await context.LotterySessions.AnyAsync(cancellationToken))
        {
            context.LotterySessions.Add(new LotterySession
            {
                SessionNumber = 1,
                StartTime = now,
                Status = SessionStatus.Active
            });
            logger.LogInformation("Created lottery session 1.");
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
