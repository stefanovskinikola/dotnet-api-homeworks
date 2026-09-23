using System.ComponentModel.DataAnnotations;

namespace Loto3000.Services.DTOs.Auth;

/// <summary>Credentials used to request a short-lived JWT.</summary>
public sealed record LoginDto
{
    /// <summary>Gets the registered login name.</summary>
    [Required, StringLength(32)]
    public string Username { get; init; } = string.Empty;

    /// <summary>Gets the password to verify; never log this value.</summary>
    [Required, StringLength(72)]
    public string Password { get; init; } = string.Empty;
}
