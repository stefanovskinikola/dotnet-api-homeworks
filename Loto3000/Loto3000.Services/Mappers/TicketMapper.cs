using Loto3000.Domain.Models;
using Loto3000.Services.DTOs.Tickets;
using Loto3000.Services.Rules;

namespace Loto3000.Services.Mappers;

/// <summary>Maps confirmations and history using loaded session/draw navigation data.</summary>
public static class TicketMapper
{
    /// <summary>Creates a detached ticket response and calculates its result when a draw exists.</summary>
    /// <param name="ticket">A ticket with its session and optional draw loaded.</param>
    /// <param name="message">Optional confirmation instructions for the player.</param>
    /// <returns>A response with copied number lists and null results before the draw.</returns>
    public static TicketResponseDto ToDto(this Ticket ticket, string message = "")
    {
        var draw = ticket.Session.Draw;
        // Set intersection counts matching values once; all eight drawn numbers participate equally.
        int? matchedCount = draw is null ? null : ticket.Numbers.Intersect(draw.DrawnNumbers).Count();
        return new TicketResponseDto(
            ticket.Id, ticket.UserId, ticket.SessionId, ticket.Session.SessionNumber, ticket.Numbers.ToArray(),
            ticket.SubmittedAt, ticket.Session.Status.ToString(), message, draw?.DrawnNumbers.ToArray(),
            matchedCount, matchedCount.HasValue ? LotteryRules.GetPrizeName(LotteryRules.GetPrize(matchedCount.Value)) : null);
    }
}
