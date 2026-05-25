namespace RiftVeil.Application.Dtos.Games;

/// <summary>
/// Admin request to set or clear a manual VOD link for one game.
/// </summary>
public record UpdateGameVodRequest(
    string? Url,
    int? DraftOffsetSeconds = null,
    int? GameStartOffsetSeconds = null,
    int OffsetSeconds = 0);
