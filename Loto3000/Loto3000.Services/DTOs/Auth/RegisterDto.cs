using System.ComponentModel.DataAnnotations;

namespace Loto3000.Services.DTOs.Auth;

/// <summary>Public registration input; role assignment is deliberately not caller-controlled.</summary>
public sealed record RegisterDto
{
    /// <summary>Gets the login name containing 3–32 letters, digits, dots, underscores or hyphens.</summary>
    [Required, StringLength(32, MinimumLength = 3), RegularExpression(@"^[a-zA-Z0-9_.-]+$")]
    public string Username { get; init; } = string.Empty;

    /// <summary>Gets the first name displayed on the public winners board.</summary>
    [Required, StringLength(80)]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Gets the last name displayed on the public winners board.</summary>
    [Required, StringLength(80)]
    public string LastName { get; init; } = string.Empty;

    /// <summary>Gets the unique email address, normalized before persistence.</summary>
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets the password; Services also enforces complexity and BCrypt's 72 UTF-8 byte limit.</summary>
    [Required, StringLength(72, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}
