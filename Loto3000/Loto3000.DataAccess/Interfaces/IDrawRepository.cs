using Loto3000.Domain.Models;

namespace Loto3000.DataAccess.Interfaces;

/// <summary>Stages draw writes and reads persisted draw history.</summary>
public interface IDrawRepository
{
    /// <summary>Stages a draw for the unit of work to save.</summary>
    /// <param name="draw">The evaluated session outcome.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task completing when the draw is tracked, not committed.</returns>
    Task AddAsync(Draw draw, CancellationToken cancellationToken = default);
    /// <summary>Reads the newest draw with its session, without change tracking.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The latest draw, or null when no draw exists.</returns>
    Task<Draw?> GetLatestDrawAsync(CancellationToken cancellationToken = default);
}
