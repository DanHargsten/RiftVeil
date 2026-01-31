using RiftVeil.Application.Dtos.Tournaments;

namespace RiftVeil.Application.Dtos.Leagues;

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
