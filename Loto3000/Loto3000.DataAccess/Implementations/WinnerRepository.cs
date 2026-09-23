using Loto3000.DataAccess.Interfaces;
using Loto3000.DataAccess.Data;
using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Loto3000.DataAccess.Implementations;

internal sealed class WinnerRepository(Loto3000DbContext context) : IWinnerRepository
{
    /// <inheritdoc />
    public async Task AddWinnersAsync(IReadOnlyCollection<Winner> winners, CancellationToken cancellationToken = default) =>
        await context.Winners.AddRangeAsync(winners, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Winner>> GetAllWinnersAsync(CancellationToken cancellationToken = default) =>
        await context.Winners.AsNoTracking().Include(winner => winner.User)
            .Include(winner => winner.Draw).ThenInclude(draw => draw.Session)
            .OrderByDescending(winner => winner.WonAt).ThenByDescending(winner => winner.Id)
            .ToListAsync(cancellationToken);
}
