using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RiftVeil.Domain.Entities;
using RiftVeil.Infrastructure.Data;
using RiftVeil.Infrastructure.Services.Import;

namespace RiftVeil.Api.Controllers;

/// <summary>
/// HTTP API for data import and VOD enrichment (Leaguepedia, lolesports).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImportController(
    LeaguepediaImportService importService,
    RiftVeilDbContext dbContext,
    LolesportsVodEnricher vodEnricher) : ControllerBase
{
    /// <summary>
    /// Imports tournaments for the given league.
    /// </summary>
    /// <param name="leagueShortName">League short name (e.g. LEC, LCS, LCK).</param>
    [HttpPost("tournaments/{leagueShortName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportTournamentsAsync(string leagueShortName)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
        {
            return NotFound($"League '{leagueShortName}' not found.");
        }

        var leaguepediaName = MapToLeaguepediaName(leagueShortName);
        if (leaguepediaName == null)
        {
            return BadRequest($"No Leaguepedia mapping for '{leagueShortName}'.");
        }

        await importService.ImportTournamentsAsync(leaguepediaName, league.Id);

        return Ok("Import complete.");
    }

    /// <summary>
    /// Imports matches for all tournaments in the given league.
    /// </summary>
    /// <param name="leagueShortName">League short name (e.g. LEC, LCS, LCK).</param>
    [HttpPost("matches/{leagueShortName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportMatchesAsync(string leagueShortName)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
        {
            return NotFound($"League '{leagueShortName}' not found.");
        }

        await importService.ImportMatchesAsync(league.Id);
        return Ok("Match import complete.");
    }

    /// <summary>
    /// Enriches games with VOD links from the lolesports API for the given league.
    /// </summary>
    /// <param name="leagueShortName">League short name (e.g. LEC, LCS, LCK).</param>
    [HttpPost("vods/{leagueShortName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportVodsAsync(string leagueShortName)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
        {
            return NotFound($"League '{leagueShortName}' not found.");
        }

        await vodEnricher.EnrichVodsAsync(league.ShortName);
        return Ok($"VOD enrichment finished for {league.ShortName}");
    }

    private async Task<League?> FindLeagueByShortNameAsync(string leagueShortName)
    {
        var key = leagueShortName.ToUpperInvariant();
        return await dbContext.Leagues
            .FirstOrDefaultAsync(l => l.ShortName == key);
    }

    private static string? MapToLeaguepediaName(string shortName) => shortName.ToUpperInvariant() switch
    {
        "LEC" => "LoL EMEA Championship",
        "LCS" => "League of Legends Championship Series",
        "LCK" => "LoL Champions Korea",
        _ => null
    };
}
