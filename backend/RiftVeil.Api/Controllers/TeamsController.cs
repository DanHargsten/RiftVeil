using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Teams;
using RiftVeil.Domain.Entities;
using RiftVeil.Infrastructure.Data;
using RiftVeil.Infrastructure.Services.Import;

namespace RiftVeil.Api.Controllers;

/// <summary>
/// Admin API for team metadata (list, manual edit, Leaguepedia sync).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TeamsController(RiftVeilDbContext dbContext, LeaguepediaImportService importService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<TeamListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TeamListItemDto>>> GetAllAsync(
        [FromQuery] string? search = null,
        [FromQuery] string? leagueShortName = null,
        [FromQuery] bool missingIconLogo = false)
    {
        var query = dbContext.Teams.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(leagueShortName))
        {
            var leagueKey = leagueShortName.Trim().ToUpperInvariant();
            var league = await dbContext.Leagues
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ShortName == leagueKey);

            if (league == null)
                return NotFound($"League '{leagueShortName}' not found.");

            query = query.Where(team =>
                dbContext.Matches.Any(match =>
                    match.Tournament.LeagueId == league.Id
                    && (match.Team1Id == team.Id || match.Team2Id == team.Id)));
        }

        if (missingIconLogo)
            query = query.Where(team => team.IconLogoUrl == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(team =>
                team.Name.Contains(term)
                || team.ShortName.Contains(term)
                || (team.Region != null && team.Region.Contains(term)));
        }

        var teams = await query
            .OrderBy(team => team.Name)
            .Select(team => new TeamListItemDto(
                team.Id,
                team.Name,
                team.ShortName,
                team.Region,
                team.LogoUrl,
                team.IconLogoUrl,
                team.ExternalId,
                dbContext.Matches.Count(m => m.Team1Id == team.Id || m.Team2Id == team.Id)))
            .ToListAsync();

        return Ok(teams);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TeamListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamListItemDto>> GetByIdAsync(int id)
    {
        var team = await dbContext.Teams
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TeamListItemDto(
                t.Id,
                t.Name,
                t.ShortName,
                t.Region,
                t.LogoUrl,
                t.IconLogoUrl,
                t.ExternalId,
                dbContext.Matches.Count(m => m.Team1Id == t.Id || m.Team2Id == t.Id)))
            .FirstOrDefaultAsync();

        if (team == null)
            return NotFound();

        return Ok(team);
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType(typeof(TeamListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamListItemDto>> UpdateAsync(int id, [FromBody] UpdateTeamRequest request)
    {
        var team = await dbContext.Teams.FirstOrDefaultAsync(t => t.Id == id);
        if (team == null)
            return NotFound();

        if (request.Name != null)
            team.SetName(request.Name);

        if (request.ShortName != null)
            team.SetShortName(request.ShortName);

        if (request.Region != null)
            team.SetRegion(string.IsNullOrWhiteSpace(request.Region) ? null : request.Region);

        if (request.LogoUrl != null)
            team.SetLogoUrl(string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl);

        if (request.IconLogoUrl != null)
            team.SetIconLogoUrl(string.IsNullOrWhiteSpace(request.IconLogoUrl) ? null : request.IconLogoUrl);

        if (request.ExternalId != null)
            team.SetExternalId(string.IsNullOrWhiteSpace(request.ExternalId) ? null : request.ExternalId);

        await dbContext.SaveChangesAsync();

        return Ok(await ToListItemDtoAsync(team));
    }

    [HttpPost("{id:int}/sync-leaguepedia")]
    [ProducesResponseType(typeof(TeamListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamListItemDto>> SyncFromLeaguepediaAsync(int id, [FromQuery] bool overwrite = false)
    {
        var team = await dbContext.Teams.FirstOrDefaultAsync(t => t.Id == id);
        if (team == null)
            return NotFound();

        var synced = await importService.SyncTeamMetadataFromLeaguepediaAsync(team, overwrite);
        if (!synced)
        {
            return NotFound(
                $"No Leaguepedia Teams row found for '{team.Name}'. " +
                "Check lol.fandom.com for the current Cargo Name (rebrands differ from match data), " +
                "edit name/short in Admin, then Sync LP again. Do not delete the team — matches reference it by id.");
        }

        await dbContext.SaveChangesAsync();

        return Ok(await ToListItemDtoAsync(team));
    }

    /// <summary>
    /// Removes a team that is not referenced by any match. Re-import can recreate teams from Leaguepedia.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var team = await dbContext.Teams.FirstOrDefaultAsync(t => t.Id == id);
        if (team == null)
            return NotFound();

        var matchCount = await dbContext.Matches.CountAsync(m => m.Team1Id == id || m.Team2Id == id);
        if (matchCount > 0)
        {
            return Conflict(
                $"Cannot delete '{team.Name}': used in {matchCount} match(es). " +
                "Remove those matches first, or keep the team and fix metadata via Sync LP.");
        }

        dbContext.Teams.Remove(team);
        await dbContext.SaveChangesAsync();
        return NoContent();
    }

    private async Task<TeamListItemDto> ToListItemDtoAsync(Team team)
    {
        var matchCount = await dbContext.Matches.CountAsync(m => m.Team1Id == team.Id || m.Team2Id == team.Id);
        return new TeamListItemDto(
            team.Id,
            team.Name,
            team.ShortName,
            team.Region,
            team.LogoUrl,
            team.IconLogoUrl,
            team.ExternalId,
            matchCount);
    }
}
