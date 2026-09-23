namespace Loto3000.Services.DTOs.Auth;

/// <summary>A successful authentication result without password material.</summary>
/// <param name="Token">The signed JWT sent in subsequent Authorization headers.</param>
/// <param name="ExpiresAt">The token's UTC expiration instant.</param>
/// <param name="User">The authenticated user's public account details.</param>
public sealed record AuthResponseDto(string Token, DateTimeOffset ExpiresAt, UserDto User);
