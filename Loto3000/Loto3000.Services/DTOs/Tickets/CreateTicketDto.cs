using System.ComponentModel.DataAnnotations;

namespace Loto3000.Services.DTOs.Tickets;

/// <summary>Seven-number ticket input; optional identity fields must match the authenticated caller.</summary>
public sealed record CreateTicketDto
{
    /// <summary>Gets an optional owner assertion; it cannot transfer ticket ownership.</summary>
    [Range(1, int.MaxValue)]
    public int? UserId { get; init; }

    /// <summary>Gets an optional login-name assertion for the authenticated owner.</summary>
    [StringLength(32)]
    public string? Username { get; init; }

    /// <summary>Gets the displayed session identifier used by the SPA to reject stale submissions.</summary>
    [Range(1, int.MaxValue)]
    public int? SessionId { get; init; }

    /// <summary>Gets exactly seven distinct integers in [1,37], validated by the business service.</summary>
    [Required]
    public List<int> Numbers { get; init; } = [];
}
