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
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    int BestOf,
    MatchStatus Status,

    int? Team1Score,
    int? Team2Score,

    string? VodUrl,

    TournamentListItemDto Tournament
);
