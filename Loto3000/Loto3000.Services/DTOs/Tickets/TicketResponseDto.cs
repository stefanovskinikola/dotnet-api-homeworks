namespace Loto3000.Services.DTOs.Tickets;

/// <summary>Ticket confirmation or history with optional results after session completion.</summary>
/// <param name="Id">The ticket identifier.</param>
/// <param name="UserId">The owner's identifier.</param>
/// <param name="SessionId">The session database identifier.</param>
/// <param name="SessionNumber">The public session number.</param>
/// <param name="Numbers">A copy of the seven chosen numbers.</param>
/// <param name="SubmittedAt">The UTC acceptance instant.</param>
/// <param name="SessionStatus">Active or Completed.</param>
/// <param name="Message">Instructions to check the winners board after the draw.</param>
/// <param name="DrawnNumbers">The eight drawn numbers, or null before the draw.</param>
/// <param name="MatchedCount">The intersection size, or null before the draw.</param>
/// <param name="PrizeName">The prize label, including No prize, or null before the draw.</param>
public sealed record TicketResponseDto(
    int Id,
    int UserId,
    int SessionId,
    int SessionNumber,
    IReadOnlyList<int> Numbers,
    DateTimeOffset SubmittedAt,
    string SessionStatus,
    string Message,
    IReadOnlyList<int>? DrawnNumbers,
    int? MatchedCount,
    string? PrizeName);
