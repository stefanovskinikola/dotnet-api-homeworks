namespace Loto3000.Domain.Models;

/// <summary>The immutable outcome of an administrator-initiated session draw.</summary>
public sealed class Draw
{
    /// <summary>Gets or sets the database identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the completed session identifier; only one draw is allowed per session.</summary>
    public int SessionId { get; set; }
    /// <summary>Gets or sets the required session navigation, populated by persistence.</summary>
    public LotterySession Session { get; set; } = null!;
    /// <summary>Gets or sets the eight distinct drawn numbers in the inclusive range 1–37.</summary>
    public List<int> DrawnNumbers { get; set; } = [];
    /// <summary>Gets or sets the UTC instant at which the draw was evaluated.</summary>
    public DateTimeOffset DrawnAt { get; set; }
    /// <summary>Gets or sets the identifier of the administrator who authorized the draw.</summary>
    public int InitiatedByAdminId { get; set; }
}
