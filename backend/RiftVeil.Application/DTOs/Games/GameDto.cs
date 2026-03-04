using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Dtos.Games;

/// <summary>
/// Represents a single game within a match (e.g., Game 1 of a Bo3).
/// Vods is null on list endpoints for performance, populated on detail views.
/// </summary>
public record GameDto(
    int Id,
    int GameNumber,
    int? WinningTeam,
    string? VodUrl,
    List<GameVodDto>? Vods = null
);

/// <summary>
/// Represents a single VOD within a game.
/// </summary>
public record GameVodDto(
    int Id,
    VodProvider Provider,
    string? Locale,
    string Url
);
