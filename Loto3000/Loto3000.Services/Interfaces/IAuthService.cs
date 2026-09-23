using Loto3000.Services.DTOs.Auth;

namespace Loto3000.Services.Interfaces;

/// <summary>Provides player registration, credential verification and signed authentication responses.</summary>
public interface IAuthService
{
    /// <summary>Registers a Player account, regardless of caller-supplied extra fields.</summary>
    /// <param name="request">Account details and a plaintext password, used only to create a hash.</param>
    /// <param name="cancellationToken">Cancels persistence.</param>
    /// <returns>A signed token and the newly registered account.</returns>
    /// <exception cref="Loto3000.Domain.Exceptions.BusinessRuleException">Validation fails or username/email already exists.</exception>
    Task<AuthResponseDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);
    /// <summary>Verifies credentials with a generic failure message to avoid account enumeration.</summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancels the account query.</param>
    /// <returns>A signed token and authenticated account details.</returns>
    /// <exception cref="Loto3000.Domain.Exceptions.BusinessRuleException">Credentials are invalid.</exception>
    Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default);
    /// <summary>Gets the authenticated account without password material.</summary>
    /// <param name="userId">The identifier from the validated subject claim.</param>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>The current account details.</returns>
    /// <exception cref="Loto3000.Domain.Exceptions.BusinessRuleException">The account no longer exists.</exception>
    Task<UserDto> GetUserAsync(int userId, CancellationToken cancellationToken = default);
    /// <summary>Refuses production startup if bootstrap credentials are default or do not match the admin.</summary>
    /// <param name="password">The configured production bootstrap secret.</param>
    /// <param name="cancellationToken">Cancels the account query.</param>
    /// <returns>A task completing after successful verification.</returns>
    /// <exception cref="Loto3000.Domain.Exceptions.BusinessRuleException">Production bootstrap validation fails.</exception>
    Task ValidateBootstrapAdministratorAsync(string password, CancellationToken cancellationToken = default);
    /// <summary>Signs a token for an already authenticated, server-trusted account.</summary>
    /// <param name="user">Trusted account details; never accept these directly from a token request.</param>
    /// <param name="issuedAt">The UTC issuance instant used for token lifetime calculation.</param>
    /// <returns>The compact HMAC-SHA256 JWT.</returns>
    string GenerateJwtToken(UserDto user, DateTimeOffset issuedAt);
    /// <summary>Validates and hashes a password with a new salt and BCrypt cost 12.</summary>
    /// <param name="password">The plaintext secret; never log or persist it.</param>
    /// <returns>The salted BCrypt hash.</returns>
    /// <exception cref="Loto3000.Domain.Exceptions.BusinessRuleException">Password strength or UTF-8 byte limits fail.</exception>
    string HashPassword(string password);
}
