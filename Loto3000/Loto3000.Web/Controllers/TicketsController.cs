using Loto3000.Services.Interfaces;
using Loto3000.Services.DTOs.Tickets;
using Loto3000.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Loto3000.Web.Controllers;

/// <summary>Authenticated ticket submission and owner-only history endpoints.</summary>
/// <param name="ticketService">The ticket business contract.</param>
[ApiController]
[Authorize]
[Route("api/tickets")]
public sealed class TicketsController(ITicketService ticketService) : ControllerBase
{
    /// <summary>Accepts seven distinct numbers and confirms the ticket for the active session.</summary>
    /// <param name="request">Numbers and optional assertions; ownership comes from the token.</param>
    /// <param name="cancellationToken">Cancels processing without suppressing transaction cleanup.</param>
    /// <returns>201 with confirmation instructions, or 400 for invalid/stale input.</returns>
    [HttpPost]
    [EnableRateLimiting("writes")]
    public async Task<ActionResult<TicketResponseDto>> Create(CreateTicketDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await ticketService.CreateTicketAsync(User.GetUserId(), request, cancellationToken));

    /// <summary>Lists only the authenticated owner's tickets and any available draw results.</summary>
    /// <param name="cancellationToken">Cancels the history query.</param>
    /// <returns>200 with personal history, or 401 when unauthenticated.</returns>
    [HttpGet("my-tickets")]
    public async Task<ActionResult<IReadOnlyList<TicketResponseDto>>> MyTickets(CancellationToken cancellationToken) =>
        Ok(await ticketService.GetUserTicketsAsync(User.GetUserId(), cancellationToken));
}
