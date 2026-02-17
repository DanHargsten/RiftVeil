using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Application.Dtos.Matches;
using RiftVeil.Application.Mappings;
using RiftVeil.Infrastructure.Data;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Infrastructure.Services.Read;

public class MatchReadService(RiftVeilDbContext context) : IMatchReadService
{
    private readonly RiftVeilDbContext _context = context;


    /// <summary>
    /// Retrieves a list of matches based on optional filters.
    /// </summary>
    /// <param name="tournamentId">The ID of the tournament to filter by.</param>
    /// <param name="status">The status of the matches to filter by.</param>
    /// <returns>A list of match list items.</returns>
    public async Task<List<MatchListItemDto>> GetAllAsync(
        int? tournamentId = null,
        MatchStatus? status = null)
    {
        var query = _context.Matches.AsQueryable();

        if (tournamentId.HasValue)
            query = query.Where(m => m.Tournament.Id == tournamentId.Value);

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        return await query
            .OrderBy(m => m.StartsAtUtc)
            .Select(MatchProjections.ToListItemDto())
            .ToListAsync();
    }


    /// <summary>
    /// Retrieves a list of upcoming matches within a specified time frame.
    /// </summary>
    /// <param name="hoursAhead">The number of hours ahead to look for upcoming matches.</param>
    /// <returns>A list of match list items.</returns>
    public async Task<List<MatchListItemDto>> GetUpcomingAsync(int days = 7)
    {
        var hoursAhead = days * 24;
        var cutoffTime = DateTimeOffset.UtcNow.AddHours(hoursAhead);

        return await _context.Matches
            .Where(m => m.Status == MatchStatus.Scheduled)
            .Where(m => m.StartsAtUtc >= DateTimeOffset.UtcNow)
            .Where(m => m.StartsAtUtc <= cutoffTime)
            .OrderBy(m => m.StartsAtUtc)
            .Select(MatchProjections.ToListItemDto())
            .ToListAsync();
    }


    /// <summary>
    /// Retrieves a match details by its ID.
    /// </summary>
    /// <param name="id">The ID of the match to retrieve.</param>
    /// <returns>A match details DTO.</returns>
    public async Task<MatchDetailsDto?> GetByIdAsync(int id)
    {
        var match = await _context.Matches
            .Include(m => m.Tournament)
                .ThenInclude(t => t.League)
            .Include(m => m.Games)
            .FirstOrDefaultAsync(m => m.Id == id);

        return match?.ToDetailsDto();
    }
}
