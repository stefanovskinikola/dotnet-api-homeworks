namespace Loto3000.Domain.Enums;

/// <summary>Application roles embedded in validated JWT claims.</summary>
public enum UserRole
{
    /// <summary>May submit and view their own tickets.</summary>
    Player = 0,
    /// <summary>May additionally execute a draw for the active session.</summary>
    Admin = 1
}
