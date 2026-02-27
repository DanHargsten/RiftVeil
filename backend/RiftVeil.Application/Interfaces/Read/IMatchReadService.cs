using RiftVeil.Application.Dtos.Matches;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Interfaces.Read;

public interface IMatchReadService
{
    /// <summary>
    /// Retrieves a list of all matches.
    /// </summary>
    /// <param name="tournamentId">The ID of the tournament to filter matches by.</param>
    /// <param name="status">The status of the matches to filter by.</param>
    /// <param name="from">Start of the date/time range.</param>
    /// <param name="to">End of the date/time range.</param>
    /// <returns>A list of match items.</returns>
    Task<List<MatchListItemDto>> GetAllAsync(
        int? tournamentId = null,
        MatchStatus? status = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null
    );

    /// <summary>
    /// Retrieves a list of upcoming matches.
    /// </summary>
    /// <param name="days">The number of days ahead to look for upcoming matches.</param>
    /// <returns>A list of match items.</returns>
    Task<List<MatchListItemDto>> GetUpcomingAsync(int days = 7);
    
    /// <summary>
    /// Retrieves a list of recent matches.
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    Task<List<MatchListItemDto>> GetRecentAsync(int count = 10);

    /// <summary>
    /// Retrieves a list of live matches.
    /// </summary>
    /// <returns></returns>
    Task<List<MatchListItemDto>> GetLiveAsync();

    /// <summary>
    /// Retrieves details of a specific match.
    /// </summary>
    /// <param name="id">The ID of the match to retrieve details for.</param>
    /// <returns>The match details, or null if not found.</returns>
    Task<MatchDetailsDto?> GetByIdAsync(int id);
}
