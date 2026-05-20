using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Teams;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
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
    LolesportsVodEnricher vodEnricher,
    GameDetailImportService gameDetailImportService) : ControllerBase
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

        await importService.ImportTournamentsAsync(leaguepediaName, league.Id, league.ShortName);

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
    /// Imports matches only for ongoing tournaments in the given league.
    /// </summary>
    [HttpPost("matches/{leagueShortName}/ongoing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportOngoingMatchesAsync(string leagueShortName)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
        {
            return NotFound($"League '{leagueShortName}' not found.");
        }

        await importService.ImportOngoingMatchesAsync(league.Id);
        return Ok("Ongoing match import complete.");
    }

    /// <summary>
    /// Imports matches for tournaments that overlap the last <paramref name="days"/> days.
    /// </summary>
    [HttpPost("matches/{leagueShortName}/recent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportRecentMatchesAsync(
        string leagueShortName,
        [FromQuery] int days = ImportTournamentFilter.DefaultRecentDays)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
        {
            return NotFound($"League '{leagueShortName}' not found.");
        }

        await importService.ImportRecentMatchesAsync(league.Id, days);
        return Ok($"Match import complete for the last {days} day(s).");
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

        await vodEnricher.EnrichVodsAsync(league.ShortName, ongoingOnly: false);
        return Ok($"VOD enrichment finished for {league.ShortName}");
    }

    /// <summary>
    /// Enriches VODs only for ongoing tournaments in the given league.
    /// </summary>
    [HttpPost("vods/{leagueShortName}/ongoing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportOngoingVodsAsync(string leagueShortName)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
        {
            return NotFound($"League '{leagueShortName}' not found.");
        }

        await vodEnricher.EnrichVodsAsync(league.ShortName, ongoingOnly: true);
        return Ok($"Ongoing VOD enrichment finished for {league.ShortName}");
    }

    /// <summary>
    /// Enriches VODs for tournaments that overlap the last <paramref name="days"/> days.
    /// </summary>
    [HttpPost("vods/{leagueShortName}/recent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportRecentVodsAsync(
        string leagueShortName,
        [FromQuery] int days = ImportTournamentFilter.DefaultRecentDays)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
        {
            return NotFound($"League '{leagueShortName}' not found.");
        }

        await vodEnricher.EnrichVodsAsync(league.ShortName, recentDays: days);
        return Ok($"VOD enrichment finished for the last {days} day(s) in {league.ShortName}.");
    }

    /// <summary>
    /// Backfills team LogoUrl, IconLogoUrl, Region, Short, and ExternalId from Leaguepedia for all teams.
    /// </summary>
    [HttpPost("backfill-teams")]
    [ProducesResponseType(typeof(TeamBackfillResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TeamBackfillResultDto>> BackfillAllTeamMetadataAsync(
        [FromQuery] bool overwrite = false)
    {
        var result = await importService.BackfillTeamMetadataAsync(leagueId: null, overwrite);
        return Ok(result);
    }

    /// <summary>
    /// Backfills team metadata for teams that appear in matches for the given league.
    /// </summary>
    [HttpPost("backfill-teams/{leagueShortName}")]
    [ProducesResponseType(typeof(TeamBackfillResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamBackfillResultDto>> BackfillTeamMetadataForLeagueAsync(
        string leagueShortName,
        [FromQuery] bool overwrite = false)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
            return NotFound($"League '{leagueShortName}' not found.");

        var result = await importService.BackfillTeamMetadataAsync(league.Id, overwrite);
        return Ok(result);
    }

    [HttpPost("backfill-game-ids/{leagueShortName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BackfillGameExternalIds(string leagueShortName)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
            return NotFound($"League '{leagueShortName}' not found.");

        var (gamesUpdated, tournamentsSkipped) = await importService.BackfillGameExternalIdsAsync(league.Id);
        return Ok(new { gamesUpdated, tournamentsSkipped });
    }
    
    /// <summary>
    /// Backfills Team1Side and Team2Side for games missing side data.
    /// </summary>
    [HttpPost("backfill-game-sides/{leagueShortName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BackfillGameSides(string leagueShortName)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
            return NotFound($"League '{leagueShortName}' not found.");

        var (gamesUpdated, tournamentsSkipped) = await importService.BackfillGameSidesAsync(league.Id);
        return Ok(new { gamesUpdated, tournamentsSkipped });
    }

    /// <summary>
    /// Imports game details (player stats, team stats, draft) for all ongoing tournaments across all leagues.
    /// </summary>
    [HttpPost("game-details/ongoing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportOngoingGameDetailsAsync()
    {
        var ongoingTournaments = await dbContext.Tournaments
            .Where(t => t.Status == TournamentStatus.Ongoing && t.LiquipediaSlug != null)
            .ToListAsync();

        Console.WriteLine($"Found {ongoingTournaments.Count} ongoing tournaments");

        foreach (var tournament in ongoingTournaments)
        {
            Console.WriteLine($"Importing game details for: {tournament.Name}");
            await gameDetailImportService.ImportGameDetailsForTournamentAsync(tournament.LiquipediaSlug!);
        }

        return Ok($"Game detail import complete for {ongoingTournaments.Count} ongoing tournaments.");
    }

    /// <summary>
    /// Imports game details for the given league. Use ongoingOnly=false for historical backfill.
    /// </summary>
    [HttpPost("game-details/{leagueShortName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportGameDetailsForLeagueAsync(
        string leagueShortName,
        [FromQuery] bool ongoingOnly = true,
        [FromQuery] int? recentDays = null)
    {
        var league = await FindLeagueByShortNameAsync(leagueShortName);
        if (league == null)
        {
            return NotFound($"League '{leagueShortName}' not found.");
        }

        var utcNow = DateTimeOffset.UtcNow;
        var tournamentsQuery = dbContext.Tournaments.Where(t => t.LeagueId == league.Id);

        string scopeLabel;
        if (recentDays is > 0)
        {
            tournamentsQuery = ImportTournamentFilter.WhereRecent(tournamentsQuery, utcNow, recentDays.Value);
            scopeLabel = $"last {recentDays.Value} day(s)";
        }
        else if (ongoingOnly)
        {
            tournamentsQuery = ImportTournamentFilter.WhereOngoingByStatus(tournamentsQuery);
            scopeLabel = "ongoing";
        }
        else
        {
            tournamentsQuery = tournamentsQuery.Where(t => t.LiquipediaSlug != null);
            scopeLabel = "all";
        }

        var tournaments = await tournamentsQuery
            .OrderByDescending(t => t.StartsAtUtc)
            .ToListAsync();

        Console.WriteLine($"Found {tournaments.Count} tournament(s) for {league.ShortName} ({scopeLabel})");

        foreach (var tournament in tournaments)
        {
            Console.WriteLine($"Importing game details for: {tournament.Name}");
            await gameDetailImportService.ImportGameDetailsForTournamentAsync(tournament.LiquipediaSlug!);
        }

        return Ok($"Game detail import complete for {tournaments.Count} tournament(s) in {league.ShortName} ({scopeLabel}).");
    }

    /// <summary>
    /// Imports game details for tournaments overlapping the last <paramref name="days"/> days.
    /// </summary>
    [HttpPost("game-details/{leagueShortName}/recent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ImportRecentGameDetailsAsync(
        string leagueShortName,
        [FromQuery] int days = ImportTournamentFilter.DefaultRecentDays) =>
        ImportGameDetailsForLeagueAsync(leagueShortName, ongoingOnly: false, recentDays: days);

    /// <summary>
    /// Imports game details for one specific tournament by local database id.
    /// </summary>
    [HttpPost("game-details/tournament/{tournamentId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportGameDetailsForTournamentIdAsync(int tournamentId)
    {
        var tournament = await dbContext.Tournaments
            .Where(t => t.Id == tournamentId)
            .Select(t => new { t.Id, t.Name, t.LiquipediaSlug })
            .FirstOrDefaultAsync();

        if (tournament == null)
            return NotFound($"Tournament {tournamentId} not found.");

        if (string.IsNullOrWhiteSpace(tournament.LiquipediaSlug))
            return BadRequest($"Tournament {tournamentId} has no Liquipedia slug.");

        await gameDetailImportService.ImportGameDetailsForTournamentAsync(tournament.LiquipediaSlug);
        return Ok($"Game detail import complete for tournament {tournament.Name} ({tournament.Id}).");
    }
    
    /// <summary>
    /// Imports player stats, team stats, and draft entries for all games
    /// in the given tournament. Use the Leaguepedia OverviewPage slug,
    /// e.g. "LEC/2026 Season/Spring Season".
    /// URL-encode slashes: LEC%2F2026+Season%2FSpring+Season
    /// </summary>
    [HttpPost("game-details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportGameDetailsAsync([FromQuery] string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return BadRequest("Query parameter 'slug' is required. Example: ?slug=LEC/2026 Season/Spring Season");

        await gameDetailImportService.ImportGameDetailsForTournamentAsync(slug);
        return Ok($"Game detail import complete for: {slug}");
    }

    /// <summary>
    /// Imports player stats, team stats, and draft for a single game by local database id (admin/testing).
    /// </summary>
    [HttpPost("game-details/game/{gameId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ImportGameDetailsForGameAsync(int gameId)
    {
        try
        {
            var message = await gameDetailImportService.ImportGameDetailsForGameIdAsync(gameId);
            if (message == null)
                return NotFound($"Game {gameId} not found.");

            return Ok(message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
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
        "LPL" => "LPL",
        "CBLOL" => "CBLOL",
        "LCP" => "LCP",
        _ => null
    };
}
