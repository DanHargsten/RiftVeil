using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Dtos.Tournaments;

/// <summary>
/// Represents a tournament list item.
/// </summary>
public record TournamentListItemDto(
    int Id,
    int LeagueId,
    string Name,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset? EndsAtUtc,
    TournamentStatus Status
);
