using Loto3000.DataAccess.Interfaces;
using Loto3000.DataAccess.Data;
using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Loto3000.DataAccess.Implementations;

internal sealed class DrawRepository(Loto3000DbContext context) : IDrawRepository
{
    /// <inheritdoc />
    public async Task AddAsync(Draw draw, CancellationToken cancellationToken = default) =>
        await context.Draws.AddAsync(draw, cancellationToken);

    /// <inheritdoc />
    public Task<Draw?> GetLatestDrawAsync(CancellationToken cancellationToken = default) =>
        context.Draws.AsNoTracking().Include(draw => draw.Session)
            .OrderByDescending(draw => draw.DrawnAt).ThenByDescending(draw => draw.Id)
            .FirstOrDefaultAsync(cancellationToken);
}
