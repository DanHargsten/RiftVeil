using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Matches;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Application.Mappings;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Read;

/// <summary>
/// Read-side queries for matches (list filters, live/upcoming/recent, and detail with games).
/// </summary>
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
            query = query.Where(match => match.TournamentId == tournamentId.Value);
        }
        else
        {
            // Date range filter
            if (from.HasValue)
            {
                query = query.Where(match => match.StartsAtUtc >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(match => match.StartsAtUtc <= to.Value);
            }
        }

        if (status.HasValue)
        {
            query = query.Where(match => match.Status == status.Value);
        }

        var now = DateTimeOffset.UtcNow;
        return await query
            .OrderBy(match => match.StartsAtUtc)
            .Select(MatchProjections.ToListItemDto(now))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<MatchListItemDto>> GetUpcomingAsync(int days = 7)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoffTime = now.AddDays(days);

        return await _context.Matches
            .Where(match => match.Status == MatchStatus.Scheduled)
            .Where(match => match.StartsAtUtc >= now)
            .Where(match => match.StartsAtUtc <= cutoffTime)
            .OrderBy(match => match.StartsAtUtc)
            .Select(MatchProjections.ToListItemDto(now))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<MatchListItemDto>> GetRecentAsync(int count = 10)
    {
        var now = DateTimeOffset.UtcNow;
        return await _context.Matches
            .Where(match => match.Status == MatchStatus.Finished)
            .OrderByDescending(match => match.StartedAtUtc)
            .Take(count)
            .Select(MatchProjections.ToListItemDto(now))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<MatchListItemDto>> GetLiveAsync()
    {
        // The import pipeline does not call Match.MarkLive yet, so Status == Live alone is not
        // sufficient. Mirror the projection heuristic here so /api/matches/live returns the same
        // /api/matches/live returns the same set of "currently playing" matches that
        // any list-view would show with a LIVE badge. See MatchProjections for context.
        var now = DateTimeOffset.UtcNow;
        return await _context.Matches
            .Where(match =>
                match.Status == MatchStatus.Live
                || (match.Status == MatchStatus.Scheduled
                    && match.StartsAtUtc <= now
                    && match.StartsAtUtc.AddMinutes(
                        match.BestOf * (double)MatchProjections.LiveWindowMinutesPerGame) >= now))
            .OrderBy(match => match.StartsAtUtc)
            .Select(MatchProjections.ToListItemDto(now))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<MatchDetailsDto?> GetByIdAsync(int id)
    {
        var match = await _context.Matches
            .Include(match => match.Tournament)
                .ThenInclude(tournament => tournament.League)
            .Include(match => match.Team1)
            .Include(match => match.Team2)
            .Include(match => match.Games)
                .ThenInclude(game => game.Vods)
            .FirstOrDefaultAsync(match => match.Id == id);

        return match?.ToDetailsDto(DateTimeOffset.UtcNow);
    }
}
