using RiftVeil.Application.DTOs.Tournaments;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.DTOs.Matches;

/// <summary>
/// Represents match details.
/// </summary>
public record MatchDetailsDto(
    int Id,
    string Team1Name,
    string Team2Name,
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
