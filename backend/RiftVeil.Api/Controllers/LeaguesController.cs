// RiftVeil.Api/Controllers/LeaguesController.cs
using Microsoft.AspNetCore.Mvc;
using RiftVeil.Application.Dtos.Leagues;
using RiftVeil.Application.Interfaces.Read;

namespace RiftVeil.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaguesController : ControllerBase
{
    private readonly ILeagueReadService _leagueReadService;

    public LeaguesController(ILeagueReadService leagueReadService)
    {
        _leagueReadService = leagueReadService;
    }

    /// <summary>
    /// Get all leagues.
    /// </summary>
    /// <returns>A list of leagues.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<LeagueListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LeagueListItemDto>>> GetAllLeaguesAsync()
    {
        var leagues = await _leagueReadService.GetAllAsync();
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
        var league = await _leagueReadService.GetByIdAsync(id);
        
        if (league == null)
            return NotFound();
        
        return Ok(league);
    }
}