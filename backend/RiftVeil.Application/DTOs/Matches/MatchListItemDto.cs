using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.DTOs.Matches;

/// <summary>
/// Represents a match list item.
/// </summary>
public record MatchListItemDto(
    int Id,
    int TournamentId,
    string TournamentName,
    string Team1Name,
    string Team2Name,
    DateTimeOffset StartsAtUtc,
    int BestOf,
    MatchStatus Status
);
