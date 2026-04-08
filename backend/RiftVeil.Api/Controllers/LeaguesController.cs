using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Leagues;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Domain.Entities;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Api.Controllers;

/// <summary>
/// HTTP API for leagues (read and create).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LeaguesController(ILeagueReadService leagueReadService, RiftVeilDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Get all leagues.
    /// </summary>
    /// <returns>A list of leagues.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<LeagueListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LeagueListItemDto>>> GetAllLeaguesAsync()
    {
        var leagues = await leagueReadService.GetAllAsync();
        return Ok(leagues);
    }

    /// <summary>
    /// Get a specific league by ID.
    /// </summary>
    /// <param name="id">The league ID.</param>
    /// <returns>The league details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LeagueDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LeagueDetailsDto>> GetByIdAsync(int id)
    {
        var league = await leagueReadService.GetByIdAsync(id);

        if (league == null)
        {
            return NotFound();
        }

        return Ok(league);
    }

    /// <summary>
    /// Create a new league.
    /// </summary>
    /// <param name="request">League fields.</param>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateLeagueRequest request)
    {
        var exists = await dbContext.Leagues
            .AnyAsync(l => l.ShortName == request.ShortName.ToUpperInvariant());

        if (exists)
        {
            return Conflict($"League '{request.ShortName}' already exists.");
        }

        var league = new League(request.Name, request.ShortName, request.Region, logoUrl: null, request.ExternalId);
        dbContext.Leagues.Add(league);
        await dbContext.SaveChangesAsync();

        return Created($"/api/leagues/{league.Id}", null);
    }
}
