using Loto3000.DataAccess.Interfaces;
using Loto3000.DataAccess.Data;
using Loto3000.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Loto3000.DataAccess.Implementations;

internal sealed class TicketRepository(Loto3000DbContext context) : ITicketRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Ticket>> GetTicketsBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default) =>
        await context.Tickets.Include(ticket => ticket.User)
            .Where(ticket => ticket.SessionId == sessionId).OrderBy(ticket => ticket.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Ticket>> GetUserTicketsAsync(int userId, CancellationToken cancellationToken = default) =>
        await context.Tickets.AsNoTracking().Include(ticket => ticket.Session).ThenInclude(session => session.Draw)
            .Where(ticket => ticket.UserId == userId)
            .OrderByDescending(ticket => ticket.SubmittedAt).ThenByDescending(ticket => ticket.Id)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public Task<int> CountBySessionIdAsync(int sessionId, CancellationToken cancellationToken = default) =>
        context.Tickets.CountAsync(ticket => ticket.SessionId == sessionId, cancellationToken);

    /// <inheritdoc />
    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default) =>
        await context.Tickets.AddAsync(ticket, cancellationToken);
}
