using Loto3000.Domain.Models;
using Loto3000.Services.DTOs.Sessions;

namespace Loto3000.Services.Mappers;

/// <summary>Maps session lifecycle data without exposing navigation graphs.</summary>
public static class SessionMapper
{
    /// <summary>Creates a session snapshot with a separately queried ticket count.</summary>
    /// <param name="session">The session to project.</param>
    /// <param name="ticketCount">The accepted ticket count at query time.</param>
    /// <returns>The public session snapshot.</returns>
    public static SessionDto ToDto(this LotterySession session, int ticketCount) =>
        new(session.Id, session.SessionNumber, session.StartTime, session.EndTime, session.Status.ToString(), ticketCount);
}
