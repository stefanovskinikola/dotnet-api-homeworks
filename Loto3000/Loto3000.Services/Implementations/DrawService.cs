using Loto3000.DataAccess.Interfaces;
using Loto3000.Domain.Models;
using Loto3000.Domain.Enums;
using Loto3000.Domain.Exceptions;
using Loto3000.Services.Interfaces;
using Loto3000.Services.DTOs.Draws;
using Loto3000.Services.DTOs.Sessions;
using Loto3000.Services.Mappers;
using Loto3000.Services.Rules;
using Microsoft.Extensions.Logging;

namespace Loto3000.Services.Implementations;

/// <summary>Evaluates a cryptographic draw and atomically closes and replaces the active session.</summary>
/// <param name="sessions">Session lookup, locking and writes.</param>
/// <param name="tickets">Entries to evaluate.</param>
/// <param name="draws">Draw persistence.</param>
/// <param name="winners">Prize result persistence.</param>
/// <param name="users">Independent administrator-role verification.</param>
/// <param name="unitOfWork">The shared serializable transaction boundary.</param>
/// <param name="logger">Structured draw diagnostics.</param>
public sealed class DrawService(
    ISessionRepository sessions,
    ITicketRepository tickets,
    IDrawRepository draws,
    IWinnerRepository winners,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ILogger<DrawService> logger) : IDrawService
{
    /// <inheritdoc />
    public async Task<SessionDto> GetCurrentSessionAsync(CancellationToken cancellationToken = default)
    {
        var session = await sessions.GetActiveSessionAsync(cancellationToken)
            ?? throw new BusinessRuleException("There is no active lottery session.");
        var ticketCount = await tickets.CountBySessionIdAsync(session.Id, cancellationToken);
        return session.ToDto(ticketCount);
    }

    /// <inheritdoc />
    public async Task<DrawResultDto> InitiateDrawAsync(int adminUserId, int expectedSessionId, CancellationToken cancellationToken = default)
    {
        var admin = await users.GetByIdAsync(adminUserId, cancellationToken);
        if (admin?.Role != UserRole.Admin)
        {
            throw new BusinessRuleException("Only an administrator can initiate a draw.");
        }

        try
        {
            // The draw, winners, completion and successor are one database transaction, not four commits.
            // The session lock also serializes competing draw and ticket requests until commit/rollback.
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            var session = await sessions.GetActiveSessionAsync(cancellationToken);
            if (session is null || session.Status != SessionStatus.Active)
            {
                throw new BusinessRuleException("There is no active lottery session to draw.");
            }

            if (session.Id != expectedSessionId)
            {
                throw new BusinessRuleException("This session has already been drawn or has changed. Refresh before initiating another draw.");
            }

            var sessionTickets = await tickets.GetTicketsBySessionIdAsync(session.Id, cancellationToken);
            var drawnAt = DateTimeOffset.UtcNow;
            var draw = new Draw
            {
                SessionId = session.Id,
                Session = session,
                DrawnNumbers = LotteryRules.GenerateDrawNumbers(),
                DrawnAt = drawnAt,
                InitiatedByAdminId = adminUserId
            };
            await draws.AddAsync(draw, cancellationToken);
            var winningTickets = sessionTickets.Select(ticket => LotteryRules.EvaluateTicket(ticket, draw))
                .OfType<Winner>().ToArray();
            await winners.AddWinnersAsync(winningTickets, cancellationToken);

            session.Status = SessionStatus.Completed;
            session.EndTime = drawnAt;
            session.Draw = draw;
            await sessions.UpdateAsync(session, cancellationToken);
            // Release the filtered unique Active-status key before inserting the next session, within the same transaction.
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var nextSession = new LotterySession
            {
                SessionNumber = checked(session.SessionNumber + 1),
                StartTime = drawnAt,
                Status = SessionStatus.Active
            };
            await sessions.AddAsync(nextSession, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            logger.LogInformation("Admin {AdminId} completed draw {DrawId} for session {SessionNumber}: {TicketCount} tickets, {WinnerCount} winners.",
                adminUserId, draw.Id, session.SessionNumber, sessionTickets.Count, winningTickets.Length);
            return draw.ToDto(sessionTickets.Count, winningTickets, nextSession);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Draw transaction did not complete for session {SessionId}.", expectedSessionId);
            try
            {
                // A canceled HTTP request must not prevent rollback; preserve the original exception.
                await unitOfWork.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                logger.LogError(rollbackException, "Draw transaction rollback failed.");
            }

            throw;
        }
    }
}
