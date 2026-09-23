namespace Loto3000.Domain.Enums;

/// <summary>The persisted lifecycle states of a lottery session.</summary>
public enum SessionStatus
{
    /// <summary>Accepts tickets until its administrator-initiated draw.</summary>
    Active = 0,
    /// <summary>Has a final draw and no longer accepts tickets.</summary>
    Completed = 1
}
