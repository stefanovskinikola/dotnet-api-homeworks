using Loto3000.Services.DTOs.Tickets;

namespace Loto3000.Services.Interfaces;

/// <summary>Validates and accepts authenticated player entries without exposing database APIs.</summary>
public interface ITicketService
{
    /// <summary>Accepts exactly seven unique pool numbers into the active session under a transaction.</summary>
    /// <param name="authenticatedUserId">The owner from the validated token, not the request body.</param>
    /// <param name="request">The chosen numbers and optional owner/session assertions.</param>
    /// <param name="cancellationToken">Cancels processing; cleanup still attempts rollback.</param>
    /// <returns>A committed ticket confirmation with instructions to check the winners board.</returns>
    /// <exception cref="Loto3000.Domain.Exceptions.BusinessRuleException">Numbers, ownership or session assertions are invalid.</exception>
    Task<TicketResponseDto> CreateTicketAsync(int authenticatedUserId, CreateTicketDto request, CancellationToken cancellationToken = default);
    /// <summary>Reads only the authenticated owner's tickets.</summary>
    /// <param name="authenticatedUserId">The owner identifier from the validated subject claim.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>Personal ticket history including available draw results.</returns>
    Task<IReadOnlyList<TicketResponseDto>> GetUserTicketsAsync(int authenticatedUserId, CancellationToken cancellationToken = default);
}
