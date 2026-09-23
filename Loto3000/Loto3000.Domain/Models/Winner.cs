using Loto3000.Domain.Enums;

namespace Loto3000.Domain.Models;

/// <summary>A persisted prize-bearing ticket result; non-winning tickets have no winner row.</summary>
public sealed class Winner
{
    /// <summary>Gets or sets the database identifier.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the draw that awarded this prize.</summary>
    public int DrawId { get; set; }
    /// <summary>Gets or sets the required draw navigation.</summary>
    public Draw Draw { get; set; } = null!;
    /// <summary>Gets or sets the winning ticket identifier.</summary>
    public int TicketId { get; set; }
    /// <summary>Gets or sets the required winning ticket navigation.</summary>
    public Ticket Ticket { get; set; } = null!;
    /// <summary>Gets or sets the winning player's identifier.</summary>
    public int UserId { get; set; }
    /// <summary>Gets or sets the required winner navigation used for the public full name.</summary>
    public User User { get; set; } = null!;
    /// <summary>Gets or sets the sorted set intersection of ticket and draw numbers.</summary>
    public List<int> MatchedNumbers { get; set; } = [];
    /// <summary>Gets or sets the intersection cardinality, from three through seven.</summary>
    public int MatchedCount { get; set; }
    /// <summary>Gets or sets the prize determined by the match count.</summary>
    public PrizeType Prize { get; set; }
    /// <summary>Gets or sets the UTC draw instant at which the prize was awarded.</summary>
    public DateTimeOffset WonAt { get; set; }
}
