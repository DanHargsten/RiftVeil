using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Dtos.Tournaments;

/// <summary>
/// Represents a tournament list item.
/// </summary>
public record TournamentListItemDto(
    int Id,
    int LeagueId,
    string LeagueName,
    string LeagueShortName,
    string Name,
    string? Stage,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    TournamentStatus Status
);
