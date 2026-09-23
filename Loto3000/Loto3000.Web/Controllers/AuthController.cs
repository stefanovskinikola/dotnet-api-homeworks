using Loto3000.Services.Interfaces;
using Loto3000.Services.DTOs.Auth;
using Loto3000.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Loto3000.Web.Controllers;

/// <summary>Public authentication and authenticated account lookup endpoints.</summary>
/// <param name="authService">The authentication contract; controllers never access persistence.</param>
[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    /// <summary>Registers a player and returns a JWT; callers cannot select the Admin role.</summary>
    /// <param name="request">Account details and password.</param>
    /// <param name="cancellationToken">Cancels the HTTP request's work.</param>
    /// <returns>201 with authentication details, or 400 for invalid/duplicate input.</returns>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await authService.RegisterAsync(request, cancellationToken));

    /// <summary>Verifies credentials and returns a short-lived Bearer token.</summary>
    /// <param name="request">Login credentials.</param>
    /// <param name="cancellationToken">Cancels the HTTP request's work.</param>
    /// <returns>200 with authentication details, or 400 with a generic credential failure.</returns>
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto request, CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(request, cancellationToken));

    /// <summary>Returns the account represented by the validated token's subject claim.</summary>
    /// <param name="cancellationToken">Cancels the account query.</param>
    /// <returns>200 with account details; authentication failures return 401.</returns>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken) =>
        Ok(await authService.GetUserAsync(User.GetUserId(), cancellationToken));
}
