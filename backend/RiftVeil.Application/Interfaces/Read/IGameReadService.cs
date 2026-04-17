using RiftVeil.Application.Dtos.Games;

namespace RiftVeil.Application.Interfaces.Read;

public interface IGameReadService
{
    /// <summary>
    /// Retrieves full details for a specific game, including player stats,
    /// team stats, and draft entries.
    /// </summary>
    /// <param name="gameId">The ID of the game.</param>
    /// <returns>Game details, or null if not found.</returns>
    Task<GameDetailsDto?> GetDetailsByIdAsync(int gameId);
}
