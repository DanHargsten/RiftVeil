using Microsoft.AspNetCore.Mvc;
using RiftVeil.Application.Dtos.Matches;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Api.Controllers;

/// <summary>
/// HTTP API for matches (list, filters, and by id).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MatchesController(IMatchReadService matchReadService) : ControllerBase
{
    /// <summary>
    /// Get all matches with optional filters.
    /// When tournamentId is provided, all matches for that tournament are returned (from/to ignored).
    /// When no tournamentId, use from/to filter by date range.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MatchListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MatchListItemDto>>> GetAllAsync(
        [FromQuery] int? tournamentId = null,
        [FromQuery] MatchStatus? status = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null)
    {
        if (tournamentId is <= 0)
            return BadRequest("tournamentId must be a positive integer.");

        if (from.HasValue && to.HasValue && from > to)
            return BadRequest("'from' cannot be later than 'to'.");

        var matches = await matchReadService.GetAllAsync(tournamentId, status, from, to);
        return Ok(matches);
    }

    /// <summary>
    /// Get upcoming matches.
    /// </summary>
    /// <param name="days">Number of days to look ahead (default 7).</param>
    [HttpGet("upcoming")]
    [ProducesResponseType(typeof(List<MatchListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MatchListItemDto>>> GetUpcomingAsync(
        [FromQuery] int days = 7)
    {
        if (days is < 1 or > 90)
            return BadRequest("days must be between 1 and 90.");

        var matches = await matchReadService.GetUpcomingAsync(days);
        return Ok(matches);
    }

    /// <summary>
    /// Get recent matches.
    /// </summary>
    /// <param name="count">Maximum number of matches to return (default 10).</param>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(List<MatchListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<MatchListItemDto>>> GetRecentAsync(
        [FromQuery] int count = 10)
    {
        if (count is < 1 or > 100)
            return BadRequest("count must be between 1 and 100.");

        var matches = await matchReadService.GetRecentAsync(count);
        return Ok(matches);
    }

    /// <summary>
    /// Get live matches.
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(typeof(List<MatchListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MatchListItemDto>>> GetLiveAsync()
    {
        var matches = await matchReadService.GetLiveAsync();
        return Ok(matches);
    }

    /// <summary>
    /// Get a specific match by ID.
    /// </summary>
    /// <param name="id">The match ID.</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(MatchDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MatchDetailsDto>> GetByIdAsync(int id)
    {
        var match = await matchReadService.GetByIdAsync(id);

        if (match == null)
        {
            return NotFound();
        }

        return Ok(match);
    }
}
