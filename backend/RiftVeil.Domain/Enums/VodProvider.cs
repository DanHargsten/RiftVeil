namespace RiftVeil.Domain.Enums;

/// <summary>
/// Supported VOD streaming providers.
/// Stored as int in DB for compact indexing.
/// </summary>
public enum VodProvider
{
    YouTube = 0,
    Twitch = 1
}