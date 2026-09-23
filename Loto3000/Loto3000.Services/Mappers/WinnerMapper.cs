using Loto3000.Domain.Models;
using Loto3000.Services.DTOs.Winners;
using Loto3000.Services.Rules;

namespace Loto3000.Services.Mappers;

/// <summary>Maps prize results for anonymous public display.</summary>
public static class WinnerMapper
{
    /// <summary>Projects a winner without exposing email addresses or authentication data.</summary>
    /// <param name="winner">A result with user, draw and session navigation properties loaded.</param>
    /// <returns>The public board entry with a copy of the matched numbers.</returns>
    public static WinnerBoardDto ToDto(this Winner winner) =>
        new(winner.TicketId, $"{winner.User.FirstName} {winner.User.LastName}", winner.MatchedNumbers.ToArray(),
            winner.MatchedCount, LotteryRules.GetPrizeName(winner.Prize), winner.Draw.DrawnAt, winner.Draw.Session.SessionNumber);
}
