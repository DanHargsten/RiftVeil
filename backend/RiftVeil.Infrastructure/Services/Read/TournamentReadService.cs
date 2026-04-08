using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Tournaments;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Application.Mappings;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Read;

public class TournamentReadService(RiftVeilDbContext context) : ITournamentReadService
{
    private readonly RiftVeilDbContext _context = context;

    /// <inheritdoc />
    public async Task<List<TournamentListItemDto>> GetAllAsync(
        int? leagueId = null,
        TournamentStatus? status = null)
    {
        var query = _context.Tournaments.AsQueryable();

        if (leagueId.HasValue)
        {
            query = query.Where(t => t.LeagueId == leagueId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        return await query
            .OrderByDescending(t => t.StartsAtUtc)
            .Select(TournamentProjections.ToListItemDto())
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TournamentDetailsDto?> GetByIdAsync(int id)
    {
        var tournament = await _context.Tournaments
            .Include(t => t.League)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Team1)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Team2)
            .Include(t => t.Matches)
                .ThenInclude(m => m.Games)
                    .ThenInclude(g => g.Vods)
            .FirstOrDefaultAsync(t => t.Id == id);

        return tournament?.ToDetailsDto();
    }
}
