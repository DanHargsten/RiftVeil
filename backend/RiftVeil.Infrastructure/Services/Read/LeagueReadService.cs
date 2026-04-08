using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Leagues;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Application.Mappings;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Read;

public class LeagueReadService(RiftVeilDbContext context) : ILeagueReadService
{
    private readonly RiftVeilDbContext _context = context;

    /// <inheritdoc />
    public async Task<List<LeagueListItemDto>> GetAllAsync()
    {
        return await _context.Leagues
            .OrderBy(l => l.Name)
            .Select(LeagueProjections.ToListItemDto())
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<LeagueDetailsDto?> GetByIdAsync(int leagueId)
    {
        var league = await _context.Leagues
            .Include(l => l.Tournaments)
            .FirstOrDefaultAsync(l => l.Id == leagueId);

        return league?.ToDetailsDto();
    }
}
