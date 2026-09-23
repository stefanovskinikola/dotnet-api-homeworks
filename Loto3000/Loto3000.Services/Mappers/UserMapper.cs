using Loto3000.Domain.Models;
using Loto3000.Services.DTOs.Auth;

namespace Loto3000.Services.Mappers;

/// <summary>Maps account data without leaking password hashes.</summary>
public static class UserMapper
{
    /// <summary>Creates an account-owner response.</summary>
    /// <param name="user">The persisted account.</param>
    /// <returns>Public account details without credential material.</returns>
    public static UserDto ToDto(this User user) =>
        new(user.Id, user.Username, user.FirstName, user.LastName, user.Email, user.Role.ToString());
}
