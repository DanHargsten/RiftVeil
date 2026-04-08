using Microsoft.AspNetCore.Mvc;
using RiftVeil.Application.Dtos.Tournaments;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Api.Controllers;

/// <summary>
/// HTTP API for tournaments (list with filters and by id).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TournamentsController(ITournamentReadService tournamentReadService) : ControllerBase
{
    /// <summary>
    /// Get all tournaments with optional filters.
    /// </summary>
    /// <param name="leagueId">When set, only tournaments for this league.</param>
    /// <param name="status">When set, only tournaments in this status.</param>
    [HttpGet]
    [ProducesResponseType(typeof(List<TournamentListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TournamentListItemDto>>> GetAllAsync(
        [FromQuery] int? leagueId = null,
        [FromQuery] TournamentStatus? status = null)
    {
        var tournaments = await tournamentReadService.GetAllAsync(leagueId, status);
        return Ok(tournaments);
    }

    /// <summary>
    /// Get a specific tournament by ID.
    /// </summary>
    /// <param name="id">The tournament ID.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(TournamentDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TournamentDetailsDto>> GetByIdAsync(int id)
    {
        var tournament = await tournamentReadService.GetByIdAsync(id);

        if (tournament == null)
        {
            return NotFound();
        }

        return Ok(tournament);
    }
}
