using Loto3000.DataAccess.Interfaces;
using Loto3000.Services.Interfaces;
using Loto3000.Services.DTOs.Winners;
using Loto3000.Services.Mappers;
using Microsoft.Extensions.Logging;

namespace Loto3000.Services.Implementations;

/// <summary>Projects persisted winners into privacy-limited public board entries.</summary>
/// <param name="winners">Winner history abstraction.</param>
/// <param name="logger">Structured query diagnostics.</param>
public sealed class WinnerService(IWinnerRepository winners, ILogger<WinnerService> logger) : IWinnerService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<WinnerBoardDto>> GetWinnersBoardAsync(CancellationToken cancellationToken = default)
    {
        var winnerRecords = await winners.GetAllWinnersAsync(cancellationToken);
        logger.LogDebug("Loaded {WinnerCount} records for the public winners board.", winnerRecords.Count);
        return winnerRecords.OrderByDescending(winner => winner.Draw.DrawnAt).ThenByDescending(winner => winner.Id)
            .Select(winner => winner.ToDto()).ToArray();
    }
}
