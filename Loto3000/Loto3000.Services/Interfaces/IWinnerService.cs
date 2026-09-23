using Loto3000.Services.DTOs.Winners;

namespace Loto3000.Services.Interfaces;

/// <summary>Provides prize-bearing results for the anonymous public board.</summary>
public interface IWinnerService
{
    /// <summary>Reads winners without exposing credentials or email addresses.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>Board entries newest draw first.</returns>
    Task<IReadOnlyList<WinnerBoardDto>> GetWinnersBoardAsync(CancellationToken cancellationToken = default);
}
