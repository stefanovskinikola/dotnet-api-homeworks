using Loto3000.Services.DTOs.Sessions;
using Loto3000.Services.DTOs.Winners;

namespace Loto3000.Services.DTOs.Draws;

/// <summary>The committed outcome and successor session returned to the administrator.</summary>
/// <param name="DrawId">The persisted draw identifier.</param>
/// <param name="SessionId">The completed session identifier.</param>
/// <param name="SessionNumber">The completed public session number.</param>
/// <param name="DrawnNumbers">The eight unique drawn numbers.</param>
/// <param name="DrawnAt">The UTC evaluation instant.</param>
/// <param name="TicketCount">The number of evaluated tickets.</param>
/// <param name="Winners">Prize-bearing results ordered by matches and ticket identifier.</param>
/// <param name="NextSession">The newly committed active session.</param>
public sealed record DrawResultDto(
    int DrawId,
    int SessionId,
    int SessionNumber,
    IReadOnlyList<int> DrawnNumbers,
    DateTimeOffset DrawnAt,
    int TicketCount,
    IReadOnlyList<WinnerBoardDto> Winners,
    SessionDto NextSession);
