namespace Loto3000.DataAccess.Interfaces;

/// <summary>Coordinates repository writes within one scoped context and an explicit transaction.</summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Starts a serializable transaction; nested transactions are not supported.</summary>
    /// <param name="cancellationToken">Cancels transaction creation.</param>
    /// <returns>A task completing when the transaction starts.</returns>
    /// <exception cref="InvalidOperationException">A transaction is already active.</exception>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    /// <summary>Flushes tracked changes without committing the enclosing transaction.</summary>
    /// <param name="cancellationToken">Cancels persistence.</param>
    /// <returns>The number of written state entries.</returns>
    /// <exception cref="Loto3000.Domain.Exceptions.BusinessRuleException">A unique database constraint is violated.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    /// <summary>Commits all saves and releases transaction-owned locks.</summary>
    /// <param name="cancellationToken">Cancels the commit request.</param>
    /// <returns>A task completing after commit and transaction disposal.</returns>
    /// <exception cref="InvalidOperationException">No transaction is active.</exception>
    Task CommitAsync(CancellationToken cancellationToken = default);
    /// <summary>Rolls back the transaction and discards tracked state, even if rollback fails.</summary>
    /// <param name="cancellationToken">Use an uncanceled token for cleanup after a canceled request.</param>
    /// <returns>A task completing after transaction cleanup.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
