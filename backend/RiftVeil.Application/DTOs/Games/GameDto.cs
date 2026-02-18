namespace RiftVeil.Application.Dtos.Games;

/// <summary>
/// Represents a single game within a match (e.g., Game 1 of a Bo3).
/// </summary>
public record GameDto(
    int Id,
    int GameNumber,
    int? WinningTeam,
    string? VodUrl
);