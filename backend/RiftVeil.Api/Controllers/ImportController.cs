using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RiftVeil.Infrastructure.Data;
using RiftVeil.Infrastructure.Services.Import;

namespace RiftVeil.Api.Controllers;

/// <summary>
/// Handles import of tournaments and matches from Leaguepedia.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ImportController(LeaguepediaImportService importService, RiftVeilDbContext dbContext) : ControllerBase
{
    /// <summary>
    /// Imports tournaments for the given league.
    /// </summary>
    /// <param name="leagueShortName">League short name (e.g. LEC, LCS, LCK).</param>
    [HttpPost("tournaments/{leagueShortName}")]
    public async Task<IActionResult> ImportTournament(string leagueShortName)
    {
        
        var league = await dbContext.Leagues
            .FirstOrDefaultAsync(l => l.ShortName == leagueShortName.ToUpperInvariant());

        if (league == null)
            return NotFound($"League '{leagueShortName}' not found.");

        var leaguepediaName = MapToLeaguepediaName(leagueShortName);
        if (leaguepediaName == null)
            return BadRequest($"No Leaguepedia mapping for '{leagueShortName}'.");
        
        await importService.ImportTournamentsAsync(leaguepediaName, league.Id);
        
        return Ok("Import complete.");
    }

    /// <summary>
    /// Imports matches for all tournaments in the given league.
    /// </summary>
    /// <param name="leagueShortName">League short name (e.g. LEC, LCS, LCK).</param>
    [HttpPost("matches/{leagueShortName}")]
    public async Task<IActionResult> ImportMatch(string leagueShortName)
    {
        var league = await dbContext.Leagues
            .FirstOrDefaultAsync(l => l.ShortName == leagueShortName.ToUpperInvariant());

        if (league == null)
            return NotFound($"League '{leagueShortName}' not found.");

        await importService.ImportMatchesAsync(league.Id);
        return Ok("Match import complete.");
    }

    private static string? MapToLeaguepediaName(string shortName) => shortName.ToUpperInvariant() switch
    {
        "LEC" => "LoL EMEA Championship",
        "LCS" => "League Championship Series",
        "LCK" => "League of Legends Champions Korea",
        _ => null
    };
}