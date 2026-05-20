namespace RiftVeil.Application.Dtos.Teams;

public record TeamListItemDto(
    int Id,
    string Name,
    string ShortName,
    string? Region,
    string? LogoUrl,
    string? IconLogoUrl,
    string? ExternalId,
    int MatchCount
);
