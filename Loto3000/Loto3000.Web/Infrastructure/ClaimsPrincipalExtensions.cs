using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Loto3000.Web.Infrastructure;

/// <summary>Extracts server-validated identity claims without trusting request-body ownership fields.</summary>
internal static class ClaimsPrincipalExtensions
{
    /// <summary>Parses a positive invariant-culture subject identifier.</summary>
    /// <param name="principal">The authenticated principal.</param>
    /// <returns>The current account identifier.</returns>
    /// <exception cref="UnauthorizedAccessException">The subject claim is missing or invalid.</exception>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        if (!int.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), NumberStyles.None, CultureInfo.InvariantCulture, out var userId) || userId <= 0)
        {
            throw new UnauthorizedAccessException("A valid user identity is required.");
        }

        return userId;
    }
}
