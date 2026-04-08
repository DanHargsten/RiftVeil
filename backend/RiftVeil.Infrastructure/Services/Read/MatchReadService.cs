using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Matches;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Application.Mappings;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Read;

public class MatchReadService(RiftVeilDbContext context) : IMatchReadService
{
    private readonly RiftVeilDbContext _context = context;

    /// <inheritdoc />
    public async Task<List<MatchListItemDto>> GetAllAsync(
        int? tournamentId = null,
        MatchStatus? status = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null)
    {
        var query = _context.Matches.AsQueryable();

        if (tournamentId.HasValue)
        {
            // Tournament filter: return all matches for that tournament
            query = query.Where(m => m.TournamentId == tournamentId.Value);
        }
        else
        {
            // Date range filter
            if (from.HasValue)
            {
                query = query.Where(m => m.StartsAtUtc >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(m => m.StartsAtUtc <= to.Value);
            }
        }

        if (status.HasValue)
        {
            query = query.Where(m => m.Status == status.Value);
        }

        return await query
            .OrderBy(m => m.StartsAtUtc)
            .Select(MatchProjections.ToListItemDto())
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<MatchListItemDto>> GetUpcomingAsync(int days = 7)
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddDays(days);

        return await _context.Matches
            .Where(m => m.Status == MatchStatus.Scheduled)
            .Where(m => m.StartsAtUtc >= DateTimeOffset.UtcNow)
            .Where(m => m.StartsAtUtc <= cutoffTime)
            .OrderBy(m => m.StartsAtUtc)
            .Select(MatchProjections.ToListItemDto())
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<MatchListItemDto>> GetRecentAsync(int count = 10)
    {
        return await _context.Matches
            .Where(m => m.Status == MatchStatus.Finished)
            .OrderByDescending(m => m.StartedAtUtc)
            .Take(count)
            .Select(MatchProjections.ToListItemDto())
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<MatchListItemDto>> GetLiveAsync()
    {
        return await _context.Matches
            .Where(m => m.Status == MatchStatus.Live)
            .OrderByDescending(m => m.StartedAtUtc)
            .Select(MatchProjections.ToListItemDto())
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<MatchDetailsDto?> GetByIdAsync(int id)
    {
        var match = await _context.Matches
            .Include(m => m.Tournament)
                .ThenInclude(t => t.League)
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .Include(m => m.Games)
                .ThenInclude(g => g.Vods)
            .FirstOrDefaultAsync(m => m.Id == id);

        return match?.ToDetailsDto();
    }
}
