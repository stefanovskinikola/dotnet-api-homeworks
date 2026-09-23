using Loto3000.Domain.Models;

namespace Loto3000.DataAccess.Interfaces;

/// <summary>Provides session persistence and locking for ticket acceptance and draw closure.</summary>
public interface ISessionRepository
{
    /// <summary>Gets the active session under an update/range lock retained by an enclosing transaction.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The active session, or null if none exists.</returns>
    Task<LotterySession?> GetActiveSessionAsync(CancellationToken cancellationToken = default);
    /// <summary>Reads a session by its database identifier.</summary>
    /// <param name="id">The session identifier, not its public number.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The session, or null if absent.</returns>
    Task<LotterySession?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Stages a new session without committing it.</summary>
    /// <param name="session">The successor session.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task completing when the session is tracked.</returns>
    Task AddAsync(LotterySession session, CancellationToken cancellationToken = default);
    /// <summary>Marks an existing session for update by the unit of work.</summary>
    /// <param name="session">The session with its new lifecycle state.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task completing when the update is staged.</returns>
    Task UpdateAsync(LotterySession session, CancellationToken cancellationToken = default);
}
