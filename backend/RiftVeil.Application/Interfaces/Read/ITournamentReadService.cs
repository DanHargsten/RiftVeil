using RiftVeil.Application.Dtos.Tournaments;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Interfaces.Read;

public interface ITournamentReadService
{
    /// <summary>
    /// Retrieves a list of tournaments based on optional filters.
    /// </summary>
    /// <param name="leagueId">The ID of the league to filter by.</param>
    /// <param name="status">The status of the tournaments to filter by.</param>
    /// <returns>A list of tournament details.</returns>
    Task<List<TournamentListItemDto>> GetAllAsync(int? leagueId = null, TournamentStatus? status = null);


    /// <summary>
    /// Retrieves detailed information about a specific tournament.
    /// </summary>
    /// <param name="id">The ID of the tournament to retrieve.</param>
    /// <returns>A tournament details DTO or null if not found.</returns>
    Task<TournamentDetailsDto?> GetByIdAsync(int id);
}
