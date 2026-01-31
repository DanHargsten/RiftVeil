using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Application.Dtos.Leagues;
using RiftVeil.Application.Mappings;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Read;

public class LeagueReadService(RiftVeilDbContext context) : ILeagueReadService
{
    private readonly RiftVeilDbContext _context = context;


    /// <summary>
    /// Retrieves a list of leagues.
    /// </summary>
    /// <returns>A list of league list item DTOs.</returns>
    public async Task<List<LeagueListItemDto>> GetAllAsync()
    {
        return await _context.Leagues
            .OrderBy(l => l.Name)
            .Select(LeagueProjections.ToListItemDto())
            .ToListAsync();
    }


    /// <summary>
    /// Retrieves a league details by its ID.
    /// </summary>
    /// <param name="id">The ID of the league to retrieve.</param>
    /// <returns>A league details DTO.</returns>
    public async Task<LeagueDetailsDto?> GetByIdAsync(int leagueId)
    {
        var league = await _context.Leagues
            .Include(l => l.Tournaments)
            .FirstOrDefaultAsync(l => l.Id == leagueId);

        return league?.ToDetailsDto();
    }
}
