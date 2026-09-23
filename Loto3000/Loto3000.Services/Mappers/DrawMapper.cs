using Loto3000.Domain.Models;
using Loto3000.Services.DTOs.Draws;

namespace Loto3000.Services.Mappers;

/// <summary>Projects a committed draw and its successor without persistence dependencies.</summary>
public static class DrawMapper
{
    /// <summary>Creates the administrator's response after the draw transaction commits.</summary>
    /// <param name="draw">The persisted draw with its completed session loaded.</param>
    /// <param name="ticketCount">The number of evaluated tickets.</param>
    /// <param name="winners">Prize-bearing results with user/draw/session navigation data.</param>
    /// <param name="nextSession">The newly persisted active session.</param>
    /// <returns>A detached snapshot with winners ranked by matches, then ticket identifier.</returns>
    public static DrawResultDto ToDto(this Draw draw, int ticketCount, IEnumerable<Winner> winners, LotterySession nextSession) =>
        new(draw.Id, draw.SessionId, draw.Session.SessionNumber, draw.DrawnNumbers.ToArray(), draw.DrawnAt,
            ticketCount, winners.OrderByDescending(winner => winner.MatchedCount).ThenBy(winner => winner.TicketId)
                .Select(winner => winner.ToDto()).ToArray(), nextSession.ToDto(0));
}
