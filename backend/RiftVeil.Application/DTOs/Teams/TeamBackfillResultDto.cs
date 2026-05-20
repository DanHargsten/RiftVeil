namespace RiftVeil.Application.Dtos.Teams;

public record TeamBackfillResultDto(
    int Total,
    int Updated,
    int Skipped,
    int NotFound,
    IReadOnlyList<TeamMissingIconDto> MissingIconLogo
);
