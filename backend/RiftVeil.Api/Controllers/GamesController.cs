using Microsoft.AspNetCore.Mvc;
using RiftVeil.Application.Dtos.Games;
using RiftVeil.Application.Interfaces.Read;

namespace RiftVeil.Api.Controllers;

/// <summary>
/// HTTP API for game-level details (stats, draft).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GamesController(IGameReadService gameReadService) : ControllerBase
{
    /// <summary>
    /// Get full details for a specific game.
    /// </summary>
    /// <param name="gameId">Primary key of the game in the local database.</param>
    [HttpGet("{gameId}/details")]
    [ProducesResponseType(typeof(GameDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GameDetailsDto>> GetDetailsAsync(int gameId)
    {
        var details = await gameReadService.GetDetailsByIdAsync(gameId);

        if (details == null)
            return NotFound();

        return Ok(details);
    }
}
