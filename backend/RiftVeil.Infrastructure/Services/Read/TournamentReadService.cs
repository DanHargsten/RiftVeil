using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Tournaments;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Application.Mappings;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Read;

public class TournamentReadService(RiftVeilDbContext context) : ITournamentReadService
{
    /// <summary>
    /// Retrieves a list of tournaments based on optional filters.
    /// </summary>
    /// <param name="leagueId">The ID of the league to filter by.</param>
    /// <param name="status">The status of the tournaments to filter by.</param>
    /// <returns>A list of tournament details.</returns>
    public async Task<List<TournamentListItemDto>> GetAllAsync(
        int? leagueId = null,
        TournamentStatus? status = null)
    {
        var query = context.Tournaments.AsQueryable();

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


    /// <summary>
    /// Retrieves a tournament by its ID.
    /// </summary>
    /// <param name="id">The ID of the tournament to retrieve.</param>
    /// <returns>The tournament details or null if not found.</returns>
    public async Task<TournamentDetailsDto?> GetByIdAsync(int id)
    {
        var tournament = await context.Tournaments
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
