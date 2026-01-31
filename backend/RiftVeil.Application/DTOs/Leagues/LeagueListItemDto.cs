namespace RiftVeil.Application.Dtos.Leagues;

/// <summary>
/// Represents a league list item.
/// </summary>
public record LeagueListItemDto
(
    int Id,
    string Name,
    string ShortName,
    string? Region,
    string? LogoUrl
);
