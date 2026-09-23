namespace Loto3000.Domain.Models;

/// <summary>A player's seven-number entry belonging to exactly one lottery session.</summary>
public sealed class Ticket
{
    /// <summary>Gets or sets the database identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the authenticated owner's identifier.</summary>
    public int UserId { get; set; }
    /// <summary>Gets or sets the required owner navigation, populated by persistence.</summary>
    public User User { get; set; } = null!;
    /// <summary>Gets or sets the session that accepted the ticket.</summary>
    public int SessionId { get; set; }
    /// <summary>Gets or sets the required session navigation, populated by persistence.</summary>
    public LotterySession Session { get; set; } = null!;
    /// <summary>Gets or sets seven distinct values in [1,37], validated before persistence.</summary>
    public List<int> Numbers { get; set; } = [];
    /// <summary>Gets or sets the UTC acceptance instant.</summary>
    public DateTimeOffset SubmittedAt { get; set; }
}
