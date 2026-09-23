using Loto3000.Services.DTOs.Draws;
using Loto3000.Services.DTOs.Sessions;

namespace Loto3000.Services.Interfaces;

/// <summary>Coordinates secure drawing and the atomic session state transition.</summary>
public interface IDrawService
{
    /// <summary>Reads the currently active session for public display.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The active session and its observed ticket count.</returns>
    /// <exception cref="Loto3000.Domain.Exceptions.BusinessRuleException">No active session exists.</exception>
    Task<SessionDto> GetCurrentSessionAsync(CancellationToken cancellationToken = default);
    /// <summary>Atomically draws the expected active session, persists winners and opens its successor.</summary>
    /// <param name="adminUserId">The authenticated administrator's identifier.</param>
    /// <param name="expectedSessionId">The displayed session identifier, required to reject stale requests.</param>
    /// <param name="cancellationToken">Cancels work; rollback uses an uncanceled cleanup token.</param>
    /// <returns>The committed outcome and new active session.</returns>
    /// <exception cref="Loto3000.Domain.Exceptions.BusinessRuleException">The caller is not an admin or the session is stale/inactive.</exception>
    Task<DrawResultDto> InitiateDrawAsync(int adminUserId, int expectedSessionId, CancellationToken cancellationToken = default);
}
