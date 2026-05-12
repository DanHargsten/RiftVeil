using RiftVeil.Application.Dtos.Games;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Dtos.Matches;

/// <summary>
/// Represents a match list item.
/// </summary>
public record MatchListItemDto(
    int Id,
    int TournamentId,
    string TournamentName,
    string? TournamentStage,
    string LeagueName,
    string LeagueShortName,
    string? LeagueRegion,
    string Team1Name,
    string Team2Name,
    string Team1ShortName,
    string Team2ShortName,
    DateTimeOffset StartsAtUtc,
    int BestOf,
    MatchStatus Status,
    int? Team1Score,
    int? Team2Score,
    string? Round,
    List<GameDto> Games
);
