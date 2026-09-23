using Loto3000.Services.Interfaces;
using Loto3000.Services.DTOs.Winners;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Loto3000.Web.Controllers;

/// <summary>Anonymous access to session, winner, matched-number, prize and draw-date results.</summary>
/// <param name="winnerService">The public board business contract.</param>
[ApiController]
[AllowAnonymous]
[Route("api/winners")]
public sealed class WinnersController(IWinnerService winnerService) : ControllerBase
{
    /// <summary>Lists public prize-bearing results with no credentials or contact information.</summary>
    /// <param name="cancellationToken">Cancels the board query.</param>
    /// <returns>200 with the board entries, newest draw first.</returns>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WinnerBoardDto>>> Get(CancellationToken cancellationToken) =>
        Ok(await winnerService.GetWinnersBoardAsync(cancellationToken));
}
