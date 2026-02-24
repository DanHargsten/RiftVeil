using Microsoft.AspNetCore.Mvc;
using RiftVeil.Application.Dtos.Matches;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MatchesController(IMatchReadService matchReadService) : ControllerBase
{

    /// <summary>
    /// Get all matches with optional filters.
    /// When tournamentId is provided, all matches for that tournament are returned (from/to ignored).
    /// When no tournamentId, use from/to to filter by date range.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MatchListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MatchListItemDto>>> GetAll(
        [FromQuery] int? tournamentId = null,
        [FromQuery] MatchStatus? status = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        var matches = await matchReadService.GetAllAsync(tournamentId, status, from, to);
        return Ok(matches);
    }

    /// <summary>
    /// Get upcoming matches.
    /// </summary>
    /// <param name="days">Number of days to look ahead (default 7).</param>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(List<MatchListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MatchListItemDto>>> GetUpcoming(
        [FromQuery] int days = 7)
    {
        var matches = await matchReadService.GetUpcomingAsync(days);
        return Ok(matches);
    }

    /// <summary>
    /// Get a specific match by ID.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MatchDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchDetailsDto>> GetById(int id)
    {
        var match = await matchReadService.GetByIdAsync(id);
        
        if (match == null)
        {
            return NotFound();
        }

        return Ok(match);
    }
}