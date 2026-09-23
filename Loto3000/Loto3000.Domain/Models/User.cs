using Loto3000.Domain.Enums;

namespace Loto3000.Domain.Models;

/// <summary>An authenticated participant; credentials are never exposed through response DTOs.</summary>
public sealed class User
{
    /// <summary>Gets or sets the database identifier used in authentication claims.</summary>
    public int Id { get; set; }
    /// <summary>Gets or sets the unique normalized login name.</summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>Gets or sets the first name displayed on the public winners board.</summary>
    public string FirstName { get; set; } = string.Empty;
    /// <summary>Gets or sets the last name displayed on the public winners board.</summary>
    public string LastName { get; set; } = string.Empty;
    /// <summary>Gets or sets the unique normalized email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Gets or sets authorization privileges; public registration always creates players.</summary>
    public UserRole Role { get; set; } = UserRole.Player;
    /// <summary>Gets or sets the salted BCrypt password hash, never a plaintext password.</summary>
    public string PasswordHash { get; set; } = string.Empty;
    /// <summary>Gets or sets the UTC registration instant.</summary>
    public DateTimeOffset CreatedAt { get; set; }
}
