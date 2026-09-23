using Loto3000.Services.Interfaces;
using Loto3000.Services.DTOs.Draws;
using Loto3000.Services.DTOs.Sessions;
using Loto3000.Web.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Loto3000.Web.Controllers;

/// <summary>Public session discovery and administrator-only draw execution.</summary>
/// <param name="drawService">The transactional draw business contract.</param>
[ApiController]
[Route("api/draws")]
public sealed class DrawsController(IDrawService drawService) : ControllerBase
{
    /// <summary>Atomically draws the displayed session and activates the next numbered session.</summary>
    /// <param name="request">The expected active session identifier.</param>
    /// <param name="cancellationToken">Cancels processing; rollback is still attempted.</param>
    /// <returns>200 with the committed result; 400 for stale sessions, 401/403 for authorization failures.</returns>
    [HttpPost("initiate")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("writes")]
    public async Task<ActionResult<DrawResultDto>> Initiate(InitiateDrawDto request, CancellationToken cancellationToken) =>
        Ok(await drawService.InitiateDrawAsync(User.GetUserId(), request.SessionId, cancellationToken));

    /// <summary>Returns the active session and observed ticket count without requiring login.</summary>
    /// <param name="cancellationToken">Cancels the query.</param>
    /// <returns>200 with session details, or 400 if no active session exists.</returns>
    [HttpGet("current-session")]
    [AllowAnonymous]
    public async Task<ActionResult<SessionDto>> CurrentSession(CancellationToken cancellationToken) =>
        Ok(await drawService.GetCurrentSessionAsync(cancellationToken));
}
