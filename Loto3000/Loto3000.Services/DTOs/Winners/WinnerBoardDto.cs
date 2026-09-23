namespace Loto3000.Services.DTOs.Winners;

/// <summary>A public prize result; excludes credentials and contact information.</summary>
/// <param name="TicketId">The winning ticket identifier.</param>
/// <param name="WinnerFullName">The participant's public first and last names.</param>
/// <param name="WinningNumbers">Matched numbers, not the full draw.</param>
/// <param name="MatchedCount">The number of matches.</param>
/// <param name="PrizeName">The human-readable prize label.</param>
/// <param name="DrawDate">The UTC draw instant.</param>
/// <param name="SessionNumber">The public session number.</param>
public sealed record WinnerBoardDto(
    int TicketId,
    string WinnerFullName,
    IReadOnlyList<int> WinningNumbers,
    int MatchedCount,
    string PrizeName,
    DateTimeOffset DrawDate,
    int SessionNumber);
