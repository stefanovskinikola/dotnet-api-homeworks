using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FluentAssertions;
using Loto3000.DataAccess.Interfaces;
using Loto3000.Domain.Models;
using Loto3000.Domain.Enums;
using Loto3000.Domain.Exceptions;
using Loto3000.Services.DTOs.Auth;
using Loto3000.Services.Implementations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Loto3000.Tests;

/// <summary>Regression checks for registration, BCrypt, JWT claims and production bootstrap safety.</summary>
public sealed class AuthServiceTests
{
    private const string Password = "ValidPlayer@123";
    private static readonly string KnownHash = BCrypt.Net.BCrypt.HashPassword(Password, workFactor: 12);

    /// <summary>Registration normalizes account data, always creates a player and persists a cost-12 hash.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RegistrationCreatesAPlayerWithAHashedPasswordAndNormalizedNames()
    {
        var fixture = new Fixture();
        var response = await fixture.Service.RegisterAsync(ValidRegistration());

        fixture.SavedUser.Should().NotBeNull();
        fixture.SavedUser!.Role.Should().Be(UserRole.Player);
        fixture.SavedUser.FirstName.Should().Be("Alice");
        fixture.SavedUser.LastName.Should().Be("Player");
        fixture.SavedUser.Email.Should().Be("alice@example.com");
        fixture.SavedUser.PasswordHash.Should().NotBe(Password);
        BCrypt.Net.BCrypt.Verify(Password, fixture.SavedUser.PasswordHash).Should().BeTrue();
        fixture.SavedUser.PasswordHash.Should().MatchRegex(@"^\$2[aby]\$12\$");
        response.User.Role.Should().Be("Player");
        response.User.Id.Should().Be(42);
        response.Token.Should().NotBeNullOrWhiteSpace();
        fixture.Unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Duplicate identity fields are rejected before any write.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RegistrationRejectsAnExistingUsernameOrEmail()
    {
        var fixture = new Fixture();
        fixture.Users.Setup(repository => repository.ExistsAsync("alice", "alice@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var act = () => fixture.Service.RegisterAsync(ValidRegistration());
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*already registered*");
        fixture.Users.Verify(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Every required password-strength category is enforced.</summary>
    /// <param name="password">A password missing at least one requirement.</param>
    [Theory]
    [InlineData("short")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NOLOWERCASE1!")]
    [InlineData("nouppercase1!")]
    [InlineData("NoSymbols123")]
    [InlineData("Only Space123 ")]
    public void RejectsWeakPasswords(string password)
    {
        var fixture = new Fixture();
        var act = () => fixture.Service.HashPassword(password);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Passwords must*");
    }

    /// <summary>Multi-byte UTF-8 passwords cannot bypass BCrypt's byte limit.</summary>
    [Fact]
    public void RejectsPasswordsThatWouldBeTruncatedByBCrypt()
    {
        var fixture = new Fixture();
        var act = () => fixture.Service.HashPassword("Aa1!" + new string('é', 35));
        act.Should().Throw<BusinessRuleException>().WithMessage("*72 UTF-8 bytes*");
    }

    /// <summary>Direct service calls enforce annotations without depending on MVC.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RegistrationValidatesInputEvenWithoutMvc()
    {
        var fixture = new Fixture();
        var act = () => fixture.Service.RegisterAsync(ValidRegistration() with { Username = "bad username", Email = "not-an-email" });
        await act.Should().ThrowAsync<BusinessRuleException>();
        fixture.Unit.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Issued JWTs validate cryptographically with the expected identity, role and lifetime.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task LoginProducesAValidSignedJwtWithCorrectIdentityRoleAndExpiry()
    {
        var fixture = new Fixture();
        var response = await fixture.Service.LoginAsync(new LoginDto { Username = "alice", Password = Password });
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(response.Token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(fixture.Options.SigningKey)),
            ValidateIssuer = true, ValidIssuer = fixture.Options.Issuer,
            ValidateAudience = true, ValidAudience = fixture.Options.Audience,
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256], RoleClaimType = "role"
        }, out var validatedToken);

        principal.FindFirst("sub")!.Value.Should().Be("42");
        principal.FindFirst("unique_name")!.Value.Should().Be("alice");
        principal.IsInRole("Player").Should().BeTrue();
        principal.FindFirst("jti")!.Value.Should().NotBeNullOrEmpty();
        validatedToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(30), TimeSpan.FromSeconds(5));
        response.ExpiresAt.UtcDateTime.Should().Be(validatedToken.ValidTo);
        response.User.Email.Should().Be("alice@example.com");
    }

    /// <summary>Credential failures do not reveal whether the account exists.</summary>
    /// <param name="unknownUser">Whether the user lookup returns no account.</param>
    /// <returns>The asynchronous assertion task.</returns>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoginUsesTheSameErrorForAnUnknownUserOrWrongPassword(bool unknownUser)
    {
        var fixture = new Fixture();
        if (unknownUser)
        {
            fixture.Users.Setup(repository => repository.GetByUsernameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        }
        var act = () => fixture.Service.LoginAsync(new LoginDto { Username = "alice", Password = "WrongPassword!123" });
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("Invalid username or password.");
    }

    /// <summary>Overlong passwords are rejected before account lookup and BCrypt verification.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task LoginRejectsOverlongUtf8Passwords()
    {
        var fixture = new Fixture();
        var act = () => fixture.Service.LoginAsync(new LoginDto { Username = "alice", Password = "Aa1!" + new string('é', 35) });
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("Invalid username or password.");
        fixture.Users.Verify(repository => repository.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Tokens issued for the same account and instant receive distinct JWT identifiers.</summary>
    [Fact]
    public void GeneratesDistinctTokenIdentifiers()
    {
        var fixture = new Fixture();
        var user = new UserDto(42, "alice", "Alice", "Player", "alice@example.com", "Player");
        var now = DateTimeOffset.UtcNow;
        var handler = new JwtSecurityTokenHandler();
        var first = handler.ReadJwtToken(fixture.Service.GenerateJwtToken(user, now));
        var second = handler.ReadJwtToken(fixture.Service.GenerateJwtToken(user, now));
        first.Id.Should().NotBe(second.Id);
    }

    /// <summary>Account responses omit the password hash.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task ReturnsTheCurrentUsersPublicProfile()
    {
        var fixture = new Fixture();
        var user = await fixture.Service.GetUserAsync(42);
        user.Username.Should().Be("alice");
        user.Role.Should().Be("Player");
        typeof(UserDto).GetProperty("PasswordHash").Should().BeNull();
    }

    /// <summary>Production bootstrap succeeds with matching non-default administrator credentials.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task AcceptsMatchingNonDefaultProductionAdministratorCredentials()
    {
        var fixture = new Fixture();
        fixture.Users.Setup(repository => repository.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Username = "admin", Role = UserRole.Admin, PasswordHash = KnownHash });
        var act = () => fixture.Service.ValidateBootstrapAdministratorAsync(Password);
        await act.Should().NotThrowAsync();
    }

    /// <summary>Production refuses default, mismatched or non-administrator bootstrap identities.</summary>
    /// <param name="password">The configured bootstrap password.</param>
    /// <param name="role">The role stored for the bootstrap account.</param>
    /// <returns>The asynchronous assertion task.</returns>
    [Theory]
    [InlineData("Admin@123", UserRole.Admin)]
    [InlineData("WrongValidPassword@123", UserRole.Admin)]
    [InlineData(Password, UserRole.Player)]
    public async Task RejectsUnsafeProductionAdministratorConfiguration(string password, UserRole role)
    {
        var fixture = new Fixture();
        fixture.Users.Setup(repository => repository.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Username = "admin", Role = role, PasswordHash = KnownHash });
        var act = () => fixture.Service.ValidateBootstrapAdministratorAsync(password);
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*non-default Admin account*");
    }

    /// <summary>Changing configuration cannot silently reset an existing development admin password.</summary>
    /// <returns>The asynchronous assertion task.</returns>
    [Fact]
    public async Task RejectsPromotingADatabaseThatStillHasTheDefaultDevelopmentPassword()
    {
        var fixture = new Fixture();
        fixture.Users.Setup(repository => repository.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Username = "admin", Role = UserRole.Admin, PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12) });
        var act = () => fixture.Service.ValidateBootstrapAdministratorAsync(Password);
        await act.Should().ThrowAsync<BusinessRuleException>().WithMessage("*Seeding never resets existing passwords*");
    }

    private static RegisterDto ValidRegistration() => new()
    {
        Username = "alice", FirstName = " Alice ", LastName = " Player ", Email = "Alice@Example.com", Password = Password
    };

    private sealed class Fixture
    {
        public Mock<IUserRepository> Users { get; } = new(MockBehavior.Strict);
        public Mock<IUnitOfWork> Unit { get; } = new(MockBehavior.Strict);
        public JwtOptions Options { get; } = new("Loto3000.Tests", "Loto3000.TestClient", "TestOnly-a-long-signing-key-with-at-least-32-UTF8-bytes", 30);
        public User? SavedUser { get; private set; }
        public AuthService Service { get; }

        public Fixture()
        {
            var user = new User { Id = 42, Username = "alice", FirstName = "Alice", LastName = "Player", Email = "alice@example.com", Role = UserRole.Player, PasswordHash = KnownHash };
            Users.Setup(repository => repository.ExistsAsync("alice", "alice@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
            Users.Setup(repository => repository.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
                .Callback<User, CancellationToken>((saved, _) => { saved.Id = 42; SavedUser = saved; }).Returns(Task.CompletedTask);
            Users.Setup(repository => repository.GetByUsernameAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(user);
            Users.Setup(repository => repository.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(user);
            Unit.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            Service = new AuthService(Users.Object, Unit.Object, Options, NullLogger<AuthService>.Instance);
        }
    }
}
