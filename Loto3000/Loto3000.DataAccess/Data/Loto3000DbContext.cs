using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Loto3000.DataAccess.Data;

/// <summary>SQL Server persistence context shared by all repositories within a request scope.</summary>
/// <param name="options">Provider and connection settings supplied by dependency injection.</param>
public sealed class Loto3000DbContext(DbContextOptions<Loto3000DbContext> options) : DbContext(options)
{
    /// <summary>Gets registered users and their hashed credentials.</summary>
    public DbSet<User> Users => Set<User>();
    /// <summary>Gets the numbered session history.</summary>
    public DbSet<LotterySession> LotterySessions => Set<LotterySession>();
    /// <summary>Gets accepted player tickets.</summary>
    public DbSet<Ticket> Tickets => Set<Ticket>();
    /// <summary>Gets the single persisted outcome for each completed session.</summary>
    public DbSet<Draw> Draws => Set<Draw>();
    /// <summary>Gets prize-bearing ticket results.</summary>
    public DbSet<Winner> Winners => Set<Winner>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Loto3000DbContext).Assembly);
    }
}
