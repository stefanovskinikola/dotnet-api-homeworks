using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Loto3000.DataAccess.Interfaces;
using Loto3000.Domain.Models;
using Loto3000.Domain.Enums;
using Loto3000.Domain.Exceptions;
using Loto3000.Services.Interfaces;
using Loto3000.Services.DTOs.Auth;
using Loto3000.Services.Mappers;
using Loto3000.Services.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Loto3000.Services.Implementations;

/// <summary>Authenticates accounts through repositories and signs short-lived JWTs.</summary>
/// <param name="users">Account queries and staged writes.</param>
/// <param name="unitOfWork">The persistence commit boundary.</param>
/// <param name="jwtOptions">Approved immutable typed-configuration dependency.</param>
/// <param name="logger">Structured diagnostics excluding credentials and tokens.</param>
public sealed class AuthService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    JwtOptions jwtOptions,
    ILogger<AuthService> logger) : IAuthService
{
    private static readonly string DummyPasswordHash = BCrypt.Net.BCrypt.HashPassword("UnusableTimingOnly!42", workFactor: 12);

    /// <inheritdoc />
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        RequestValidation.Validate(request);
        RequestValidation.ValidatePassword(request.Password);
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.ExistsAsync(username, email, cancellationToken))
        {
            throw new BusinessRuleException("That username or email is already registered.");
        }

        var user = new User
        {
            Username = username,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Role = UserRole.Player,
            PasswordHash = HashPassword(request.Password),
            CreatedAt = DateTimeOffset.UtcNow
        };
        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Registered player {UserId}.", user.Id);
        return CreateAuthResponse(user);
    }

    /// <inheritdoc />
    public async Task<AuthResponseDto> LoginAsync(LoginDto request, CancellationToken cancellationToken = default)
    {
        RequestValidation.Validate(request);
        if (Encoding.UTF8.GetByteCount(request.Password) > 72)
        {
            throw new BusinessRuleException("Invalid username or password.");
        }

        var user = await users.GetByUsernameAsync(request.Username.Trim(), cancellationToken);
        // Unknown usernames still incur BCrypt work to reduce timing-based account enumeration.
        var passwordMatches = BCrypt.Net.BCrypt.Verify(request.Password, user?.PasswordHash ?? DummyPasswordHash);
        if (user is null || !passwordMatches)
        {
            throw new BusinessRuleException("Invalid username or password.");
        }

        logger.LogInformation("Authenticated user {UserId}.", user.Id);
        return CreateAuthResponse(user);
    }

    /// <inheritdoc />
    public async Task<UserDto> GetUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new BusinessRuleException("The user account no longer exists.");
        return user.ToDto();
    }

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        RequestValidation.ValidatePassword(password);
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <inheritdoc />
    public async Task ValidateBootstrapAdministratorAsync(string password, CancellationToken cancellationToken = default)
    {
        RequestValidation.ValidatePassword(password);
        var admin = await users.GetByUsernameAsync("admin", cancellationToken);
        if (password == "Admin@123" || admin?.Role != UserRole.Admin || !BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
        {
            throw new BusinessRuleException("Production bootstrap credentials must match an existing, non-default Admin account. Seeding never resets existing passwords.");
        }
    }

    /// <inheritdoc />
    public string GenerateJwtToken(UserDto user, DateTimeOffset issuedAt)
    {
        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Iat, issuedAt.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new("role", user.Role)
        ];
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            jwtOptions.Issuer, jwtOptions.Audience, claims, issuedAt.UtcDateTime,
            issuedAt.AddMinutes(jwtOptions.ExpirationMinutes).UtcDateTime, credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private AuthResponseDto CreateAuthResponse(User user)
    {
        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var userDto = user.ToDto();
        return new AuthResponseDto(GenerateJwtToken(userDto, issuedAt), issuedAt.AddMinutes(jwtOptions.ExpirationMinutes), userDto);
    }
}
