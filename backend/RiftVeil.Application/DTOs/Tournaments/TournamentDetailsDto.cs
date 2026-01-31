using RiftVeil.Application.Dtos.Leagues;
using RiftVeil.Application.Dtos.Matches;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Dtos.Tournaments;

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
