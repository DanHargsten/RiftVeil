using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Leagues;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Application.Mappings;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Read;

/// <summary>
/// Read-side queries for leagues and their tournaments.
/// </summary>
public class LeagueReadService(RiftVeilDbContext context) : ILeagueReadService
{
    private readonly RiftVeilDbContext _context = context;

    /// <inheritdoc />
    public async Task<List<LeagueListItemDto>> GetAllAsync()
    {
        return await _context.Leagues
            .OrderBy(league => league.Name)
            .Select(LeagueProjections.ToListItemDto())
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<LeagueDetailsDto?> GetByIdAsync(int leagueId)
    {
        var league = await _context.Leagues
            .Include(league => league.Tournaments)
            .FirstOrDefaultAsync(league => league.Id == leagueId);

        return league?.ToDetailsDto();
    }
}
