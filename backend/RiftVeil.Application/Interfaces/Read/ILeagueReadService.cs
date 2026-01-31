using RiftVeil.Application.Dtos.Leagues;

namespace RiftVeil.Application.Interfaces.Read;

public interface ILeagueReadService
{
    /// <summary>
    /// Retrieves a list of all leagues.
    /// </summary>
    /// <returns>A list of league items.</returns>
    Task<List<LeagueListItemDto>> GetAllAsync();


    /// <summary>
    /// Retrieves details of a specific league.
    /// </summary>
    /// <param name="leagueId">The ID of the league to retrieve.</param>
    /// <returns>The details of the league, or null if not found.</returns>
    Task<LeagueDetailsDto?> GetByIdAsync(int leagueId);
}
