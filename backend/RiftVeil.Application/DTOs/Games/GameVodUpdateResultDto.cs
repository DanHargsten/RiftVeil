namespace RiftVeil.Application.Dtos.Games;

/// <summary>
/// Result after updating a game's VOD metadata.
/// </summary>
public record GameVodUpdateResultDto(
    int GameId,
    int GameNumber,
    string? VodUrl,
    string? BaseUrl,
    int? DraftOffsetSeconds,
    int? GameStartOffsetSeconds);
