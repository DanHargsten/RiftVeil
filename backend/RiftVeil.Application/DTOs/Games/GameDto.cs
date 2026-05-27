using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Dtos.Games;

/// <summary>
/// Represents a single game within a match (e.g., Game 1 of a Bo3).
/// Vods is null on list endpoints for performance, populated on detail views.
/// </summary>
/// <param name="WinningTeam">Winning side: 1 or 2; null if the game is unfinished.</param>
public record GameDto(
    int Id,
    int GameNumber,
    int? WinningTeam,
    string? VodUrl,
    List<GameVodDto>? Vods = null,
    string? VodBaseUrl = null,
    int? VodDraftOffsetSeconds = null,
    int? VodGameStartOffsetSeconds = null
);

/// <summary>
/// Represents a single VOD within a game.
/// </summary>
public record GameVodDto(
    int Id,
    VodProvider Provider,
    VodSource Source,
    string? Locale,
    string Url,
    int? OffsetSeconds = null,
    int? DraftOffsetSeconds = null
);
