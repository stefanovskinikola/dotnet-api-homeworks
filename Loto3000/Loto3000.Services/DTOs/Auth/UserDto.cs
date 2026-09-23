namespace Loto3000.Services.DTOs.Auth;

/// <summary>Account details safe to return to the account owner; excludes password hashes.</summary>
/// <param name="Id">The database identifier used in the subject claim.</param>
/// <param name="Username">The login name.</param>
/// <param name="FirstName">The participant's first name.</param>
/// <param name="LastName">The participant's last name.</param>
/// <param name="Email">The owner's email address.</param>
/// <param name="Role">The server-assigned Player or Admin role.</param>
public sealed record UserDto(int Id, string Username, string FirstName, string LastName, string Email, string Role);
