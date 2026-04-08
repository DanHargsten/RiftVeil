using RiftVeil.Domain.Common;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Domain.Entities;

/// <summary>
/// Represents a single VOD link for a game, from a specific provider and locale.
/// A game can have multiple VOD links (e.g., YouTube en-US, Twitch en-GB).
/// </summary>
public class GameVod : BaseEntity
{
    public int GameId { get; private set; }
    public Game Game { get; private set; } = null!;

    /// <summary>
    /// The streaming provider (e.g., YouTube, Twitch).
    /// </summary>
    public VodProvider Provider { get; private set; }

    /// <summary>
    /// Full URL to the VOD.
    /// </summary>
    public string Url { get; private set; } = null!;

    /// <summary>
    /// The locale of the VOD (e.g., "en-US", "en-GB").
    /// </summary>
    public string? Locale { get; private set; }

    /// <summary>
    /// Raw video/parameter ID from the API.
    /// </summary>
    public string? Parameter { get; private set; }

    /// <summary>
    /// Offset in seconds into the VOD where the game starts.
    /// </summary>
    public int OffsetSeconds { get; private set; }

    /// <summary>
    /// Lower = more preferred. Allows admin override of auto-detection priority.
    /// </summary>
    public int Priority { get; private set; }

    // Required for EF Core materialization without exposing public setters.
    private GameVod() { }

    public GameVod(
        int gameId,
        VodProvider provider,
        string url,
        string? locale = null,
        string? parameter = null,
        int offsetSeconds = 0,
        int priority = 0)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL is required", nameof(url));

        GameId = gameId;
        Provider = provider;
        Url = url.Trim();
        Locale = ValidationUtils.NormalizeOptional(locale);
        Parameter = ValidationUtils.NormalizeOptional(parameter);
        OffsetSeconds = offsetSeconds;
        Priority = priority;
    }
}
