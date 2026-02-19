using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RiftVeil.Infrastructure.Data;
using RiftVeil.Infrastructure.Services.Import;

namespace RiftVeil.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController(LeaguepediaImportService importService, RiftVeilDbContext dbContext) : ControllerBase
{
    [HttpPost("tournaments/{leagueShortName}")]
    public async Task<IActionResult> ImportTournament(string leagueShortName)
    {
        
        var league = await dbContext.Leagues
            .FirstOrDefaultAsync(l => l.ShortName == leagueShortName.ToUpperInvariant());

        if (league == null)
            return NotFound($"League '{leagueShortName}' not found.");
        
        // Map our short names to Leaguepedia league names
        var leaguepediaName = leagueShortName.ToUpperInvariant() switch
        {
            "LEC" => "LoL EMEA Championship",
            "LCS" => "League Championship Series",
            "LCK" => "League of Legends Champions Korea",
            _ => null
        };
        
        if (leaguepediaName == null)
            return BadRequest($"No Leaguepedia mapping for '{leagueShortName}'.");
        
        await importService.ImportLeaguepediaAsync(leaguepediaName, league.Id);
        
        return Ok("Import complete.");
    }
}