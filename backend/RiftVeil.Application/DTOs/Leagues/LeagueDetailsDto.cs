using RiftVeil.Application.DTOs.Tournaments;

namespace RiftVeil.Application.DTOs.Leagues;

/// <summary>
/// Represents league details.
/// </summary>
public record LeagueDetailsDto(
    int Id,
    string Name,
    string ShortName,
    string? Region,
    string? LogoUrl,

    IReadOnlyList<TournamentListItemDto> Tournaments
);
