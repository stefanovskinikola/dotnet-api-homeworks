using Loto3000.Domain.Enums;

namespace Loto3000.Domain.Models;

/// <summary>A ticket-acceptance period that transitions once from active to completed.</summary>
public sealed class LotterySession
{
    /// <summary>Gets or sets the database identifier, distinct from the public session number.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the unique, monotonically increasing public session number.</summary>
    public int SessionNumber { get; set; }
    /// <summary>Gets or sets the UTC opening instant.</summary>
    public DateTimeOffset StartTime { get; set; }
    /// <summary>Gets or sets the UTC completion instant; null while active.</summary>
    public DateTimeOffset? EndTime { get; set; }
    /// <summary>Gets or sets the lifecycle state; the database permits only one active session.</summary>
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    /// <summary>Gets or sets the tickets accepted before session closure.</summary>
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    /// <summary>Gets or sets the single draw, or null before completion.</summary>
    public Draw? Draw { get; set; }
}
