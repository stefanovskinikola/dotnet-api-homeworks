using System.Data;
using Loto3000.DataAccess.Interfaces;
using Loto3000.DataAccess.Data;
using Loto3000.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Loto3000.DataAccess.Implementations;

/// <summary>Owns the serializable transaction shared by scoped repositories; saves do not commit it.</summary>
internal sealed class UnitOfWork(Loto3000DbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active in this scope.");
        }

        _transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            // Preserve diagnostic details in the inner exception, never in the public error text.
            throw new BusinessRuleException("The operation conflicts with existing data. Refresh and try again; usernames and emails must be unique.", exception);
        }
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("No transaction is active.");
        }

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_transaction is not null)
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            if (_transaction is not null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            // A rollback does not undo in-memory tracked values; discard them before any reuse.
            context.ChangeTracker.Clear();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
