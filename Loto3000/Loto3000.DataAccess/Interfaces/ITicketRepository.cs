using Loto3000.Domain.Models;

namespace Loto3000.DataAccess.Interfaces;

/// <summary>Persists tickets and retrieves the data needed for matching and personal history.</summary>
public interface ITicketRepository
{
    /// <summary>Loads a session's tickets with their owners for prize evaluation.</summary>
    /// <param name="sessionId">The session being drawn.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>Tickets ordered by identifier.</returns>
    Task<IReadOnlyList<Ticket>> GetTicketsBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default);
    /// <summary>Reads one owner's ticket history with session/draw details, without tracking.</summary>
    /// <param name="userId">The authenticated owner's identifier.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>Tickets newest first.</returns>
    Task<IReadOnlyList<Ticket>> GetUserTicketsAsync(int userId, CancellationToken cancellationToken = default);
    /// <summary>Counts tickets accepted into a session.</summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The ticket count.</returns>
    Task<int> CountBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default);
    /// <summary>Stages a validated ticket for persistence.</summary>
    /// <param name="ticket">The authenticated owner's entry.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task completing before the unit of work saves the entry.</returns>
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default);
}
