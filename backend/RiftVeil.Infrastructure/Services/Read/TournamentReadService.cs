using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Tournaments;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Application.Mappings;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Read;

/// <summary>
/// Read-side queries for tournaments and nested matches.
/// </summary>
public class TournamentReadService(RiftVeilDbContext context) : ITournamentReadService
{
    private readonly RiftVeilDbContext _context = context;

    /// <inheritdoc />
    public async Task<List<TournamentListItemDto>> GetAllAsync(
        int? leagueId = null,
        TournamentStatus? status = null)
    {
        var query = _context.Tournaments.AsNoTracking();

        if (leagueId.HasValue)
        {
            query = query.Where(tournament => tournament.LeagueId == leagueId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(tournament => tournament.Status == status.Value);
        }

        return await query
            .OrderByDescending(tournament => tournament.StartsAtUtc)
            .Select(TournamentProjections.ToListItemDto())
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<TournamentDetailsDto?> GetByIdAsync(int id)
    {
        var tournament = await _context.Tournaments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(tournament => tournament.League)
            .Include(tournament => tournament.Matches)
                .ThenInclude(match => match.Team1)
            .Include(tournament => tournament.Matches)
                .ThenInclude(match => match.Team2)
            .Include(tournament => tournament.Matches)
                .ThenInclude(match => match.Games)
                    .ThenInclude(game => game.Vods)
            .FirstOrDefaultAsync(tournament => tournament.Id == id);

        return tournament?.ToDetailsDto();
    }
}
