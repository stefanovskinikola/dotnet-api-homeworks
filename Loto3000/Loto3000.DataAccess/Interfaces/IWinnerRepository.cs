using Loto3000.Domain.Models;

namespace Loto3000.DataAccess.Interfaces;

/// <summary>Stages prize-bearing results and reads the public winners board.</summary>
public interface IWinnerRepository
{
    /// <summary>Stages all winners in a draw without committing independently.</summary>
    /// <param name="winners">Results with at least three matches.</param>
    /// <param name="cancellationToken">Cancels the operation.</param>
    /// <returns>A task completing when all results are tracked.</returns>
    Task AddWinnersAsync(IReadOnlyCollection<Winner> winners, CancellationToken cancellationToken = default);
    /// <summary>Reads winners with user, draw and session details without tracking.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>Winners ordered by draw time and identifier descending.</returns>
    Task<IReadOnlyList<Winner>> GetAllWinnersAsync(CancellationToken cancellationToken = default);
}
