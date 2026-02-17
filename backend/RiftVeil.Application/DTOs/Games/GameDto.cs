namespace  RiftVeil.Application.Dtos.Games;
    
public record GameDto(
    int Id,
    int GameNumber,
    int? WinningTeam,
    string? VodUrl
);