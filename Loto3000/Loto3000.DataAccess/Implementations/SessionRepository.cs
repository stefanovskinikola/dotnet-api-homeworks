using Loto3000.DataAccess.Interfaces;
using Loto3000.DataAccess.Data;
using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Loto3000.DataAccess.Implementations;

internal sealed class SessionRepository(Loto3000DbContext context) : ISessionRepository
{
    /// <inheritdoc />
    public Task<LotterySession?> GetActiveSessionAsync(CancellationToken cancellationToken = default)
    {
        // Both ticket submission and drawing hold this update lock until their transaction commits.
        return context.LotterySessions
            .FromSqlRaw("SELECT * FROM [LotterySessions] WITH (UPDLOCK, HOLDLOCK) WHERE [Status] = 0")
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<LotterySession?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        context.LotterySessions.SingleOrDefaultAsync(session => session.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(LotterySession session, CancellationToken cancellationToken = default) =>
        await context.LotterySessions.AddAsync(session, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(LotterySession session, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Entry(session).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}
