using System.ComponentModel.DataAnnotations;

namespace Loto3000.Services.DTOs.Draws;

/// <summary>Identifies the session the administrator actually intends to draw.</summary>
public sealed record InitiateDrawDto
{
    /// <summary>Gets the displayed active session identifier; stale values are rejected, never rolled forward.</summary>
    [Range(1, int.MaxValue)]
    public int SessionId { get; init; }
}
