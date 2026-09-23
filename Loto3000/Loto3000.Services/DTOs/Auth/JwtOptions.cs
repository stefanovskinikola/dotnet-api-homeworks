using System.Text;
using Microsoft.Extensions.Configuration;

namespace Loto3000.Services.DTOs.Auth;

/// <summary>Immutable authentication settings; a configuration dependency, never a persistence dependency.</summary>
/// <param name="Issuer">The expected token issuer.</param>
/// <param name="Audience">The intended API audience.</param>
/// <param name="SigningKey">A secret containing at least 32 UTF-8 bytes.</param>
/// <param name="ExpirationMinutes">The lifetime in minutes, from 1 through 120.</param>
public sealed record JwtOptions(string Issuer, string Audience, string SigningKey, int ExpirationMinutes)
{
    /// <summary>Loads validated JWT settings and fails startup if required values are invalid.</summary>
    /// <param name="configuration">The application's composed configuration.</param>
    /// <returns>Validated, immutable JWT settings.</returns>
    /// <exception cref="InvalidOperationException">Issuer, audience, signing key or lifetime is invalid.</exception>
    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var signingKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience are required.");
        }

        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be supplied securely and contain at least 32 UTF-8 bytes.");
        }

        if (!int.TryParse(configuration["Jwt:ExpirationMinutes"], out var expirationMinutes) || expirationMinutes is < 1 or > 120)
        {
            throw new InvalidOperationException("Jwt:ExpirationMinutes must be between 1 and 120.");
        }

        return new JwtOptions(issuer, audience, signingKey, expirationMinutes);
    }
}
