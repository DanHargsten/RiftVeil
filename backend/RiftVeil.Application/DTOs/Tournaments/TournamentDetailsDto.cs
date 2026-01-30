using RiftVeil.Application.DTOs.Leagues;
using RiftVeil.Application.DTOs.Matches;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.DTOs.Tournaments;

/// <summary>
/// Represents tournament details.
/// </summary>
public record TournamentDetailsDto(
    int Id,
    string Name,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    TournamentStatus Status,
    string? LiquipediaSlug,

    LeagueListItemDto League,
    IReadOnlyList<MatchListItemDto> Matches
);
