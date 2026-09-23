using Loto3000.DataAccess.Interfaces;
using Loto3000.Domain.Models;
using Loto3000.Domain.Enums;
using Loto3000.Domain.Exceptions;
using Loto3000.Services.Interfaces;
using Loto3000.Services.DTOs.Tickets;
using Loto3000.Services.Mappers;
using Loto3000.Services.Rules;
using Loto3000.Services.Validation;
using Microsoft.Extensions.Logging;

namespace Loto3000.Services.Implementations;

/// <summary>Enforces ticket ownership, number rules and transaction-safe session acceptance.</summary>
/// <param name="tickets">Ticket persistence abstraction.</param>
/// <param name="sessions">Session lookup and locking abstraction.</param>
/// <param name="users">Authenticated account lookup.</param>
/// <param name="unitOfWork">The shared transaction boundary.</param>
/// <param name="logger">Structured ticket diagnostics.</param>
public sealed class TicketService(
    ITicketRepository tickets,
    ISessionRepository sessions,
    IUserRepository users,
    IUnitOfWork unitOfWork,
    ILogger<TicketService> logger) : ITicketService
{
    /// <inheritdoc />
    public async Task<TicketResponseDto> CreateTicketAsync(int authenticatedUserId, CreateTicketDto request, CancellationToken cancellationToken = default)
    {
        RequestValidation.Validate(request);
        LotteryRules.ValidateTicketNumbers(request.Numbers);
        var user = await users.GetByIdAsync(authenticatedUserId, cancellationToken)
            ?? throw new BusinessRuleException("The user account no longer exists.");
        if ((request.UserId.HasValue && request.UserId != authenticatedUserId)
            || (request.Username is not null && !string.Equals(request.Username, user.Username, StringComparison.OrdinalIgnoreCase)))
        {
            throw new BusinessRuleException("Tickets can only be submitted for your own account.");
        }

        try
        {
            // Share the session lock with drawing so a ticket is either accepted before closure or rejected.
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            var session = await sessions.GetActiveSessionAsync(cancellationToken);
            if (session is null || session.Status != SessionStatus.Active)
            {
                throw new BusinessRuleException("There is no active lottery session. Please try again later.");
            }

            if (request.SessionId.HasValue && request.SessionId != session.Id)
            {
                throw new BusinessRuleException("The session has changed. Refresh the current session before submitting your ticket.");
            }

            var ticket = new Ticket
            {
                UserId = authenticatedUserId,
                SessionId = session.Id,
                Session = session,
                Numbers = request.Numbers.Order().ToList(),
                SubmittedAt = DateTimeOffset.UtcNow
            };
            await tickets.AddAsync(ticket, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            logger.LogInformation("Ticket {TicketId} submitted by user {UserId} to session {SessionNumber}.", ticket.Id, authenticatedUserId, session.SessionNumber);
            return ticket.ToDto("Your ticket is confirmed! Check the Winners Board after the draw to see if you won.");
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Ticket submission did not complete for user {UserId}.", authenticatedUserId);
            try
            {
                await unitOfWork.RollbackAsync(CancellationToken.None);
            }
            catch (Exception rollbackException)
            {
                logger.LogError(rollbackException, "Ticket transaction rollback failed.");
            }

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TicketResponseDto>> GetUserTicketsAsync(int authenticatedUserId, CancellationToken cancellationToken = default)
    {
        var userTickets = await tickets.GetUserTicketsAsync(authenticatedUserId, cancellationToken);
        return userTickets.Select(ticket => ticket.ToDto()).ToArray();
    }
}
