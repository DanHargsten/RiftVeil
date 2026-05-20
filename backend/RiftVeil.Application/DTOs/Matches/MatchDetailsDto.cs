using RiftVeil.Application.Dtos.Games;
using RiftVeil.Application.Dtos.Tournaments;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Dtos.Matches;

/// <summary>
/// Represents match details.
/// </summary>
public record MatchDetailsDto(
    int Id,
    string Team1Name,
    string Team2Name,
    string Team1ShortName,
    string Team2ShortName,
    string? Team1LogoUrl,
    string? Team2LogoUrl,
    string? Team1IconLogoUrl,
    string? Team2IconLogoUrl,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    int BestOf,
    MatchStatus Status,

    int? Team1Score,
    int? Team2Score,

    string? Round,
    string? VodUrl,

    TournamentListItemDto Tournament,
    List<GameDto> Games
);
