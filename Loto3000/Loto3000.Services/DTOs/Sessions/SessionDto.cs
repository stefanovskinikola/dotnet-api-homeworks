namespace Loto3000.Services.DTOs.Sessions;

/// <summary>A session's public state and current ticket count.</summary>
/// <param name="Id">The database identifier used for stale-request checks.</param>
/// <param name="SessionNumber">The sequential public session number.</param>
/// <param name="StartTime">The UTC opening instant.</param>
/// <param name="EndTime">The UTC completion instant, or null while active.</param>
/// <param name="Status">Active or Completed.</param>
/// <param name="TicketCount">The number of accepted tickets at query time.</param>
public sealed record SessionDto(int Id, int SessionNumber, DateTimeOffset StartTime, DateTimeOffset? EndTime, string Status, int TicketCount);
