namespace RiftVeil.Application.Dtos.Leagues;

/// <summary>
/// Request body for creating a league.
/// </summary>
public class CreateLeagueRequest
{
    public required string Name { get; init; }
    public required string ShortName { get; init; }
    public string? Region { get; init; }
    public string? LogoUrl { get; init; }
    public string? ExternalId { get; init; }
}
