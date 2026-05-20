namespace RiftVeil.Application.Dtos.Teams;

public record UpdateTeamRequest(
    string? Name,
    string? ShortName,
    string? Region,
    string? LogoUrl,
    string? IconLogoUrl,
    string? ExternalId
);
