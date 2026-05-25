using RiftVeil.Domain.Enums;

namespace RiftVeil.Domain.Common;

/// <summary>
/// Builds playback URLs for manual and imported game VODs.
/// </summary>
public static class GameVodUrls
{
    public static bool TryParseProvider(string url, out VodProvider provider)
    {
        provider = default;

        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return false;

        var host = uri.Host.ToLowerInvariant();

        if (host is "youtube.com" or "www.youtube.com" or "m.youtube.com" or "youtu.be")
        {
            provider = VodProvider.YouTube;
            return true;
        }

        if (host is "twitch.tv" or "www.twitch.tv" or "clips.twitch.tv")
        {
            provider = VodProvider.Twitch;
            return true;
        }

        return false;
    }

    public static string? TryExtractParameter(string url, VodProvider provider)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return null;

        if (provider == VodProvider.YouTube)
        {
            if (uri.Host.Equals("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                var id = uri.AbsolutePath.Trim('/');
                return id.Length > 0 ? id : null;
            }

            var query = uri.Query.TrimStart('?');
            foreach (var part in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = part.Split('=', 2);
                if (pair.Length == 2
                    && pair[0].Equals("v", StringComparison.OrdinalIgnoreCase)
                    && pair[1].Length > 0)
                {
                    return Uri.UnescapeDataString(pair[1]);
                }
            }

            return null;
        }

        if (provider == VodProvider.Twitch)
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var videosIndex = Array.FindIndex(segments, segment =>
                segment.Equals("videos", StringComparison.OrdinalIgnoreCase));

            if (videosIndex >= 0 && videosIndex + 1 < segments.Length)
                return segments[videosIndex + 1];

            return segments.Length > 0 ? segments[^1] : null;
        }

        return null;
    }

    public static string WithoutPlaybackOffset(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
            return url.Trim();

        var query = uri.Query.TrimStart('?');
        var kept = query.Length == 0
            ? []
            : query
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(part => !IsPlaybackQueryParam(part))
                .ToList();

        var builder = new UriBuilder(uri)
        {
            Query = kept.Count > 0 ? string.Join('&', kept) : string.Empty,
            Fragment = string.Empty,
        };

        return builder.Uri.ToString().TrimEnd('?');
    }

    public static string WithOffset(string url, int offsetSeconds, VodProvider provider)
    {
        if (offsetSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(offsetSeconds), "Offset cannot be negative.");

        var trimmed = WithoutPlaybackOffset(url);

        if (provider == VodProvider.YouTube)
        {
            var separator = trimmed.Contains('?', StringComparison.Ordinal) ? '&' : '?';
            return $"{trimmed}{separator}t={offsetSeconds}s";
        }

        var twitchSeparator = trimmed.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{trimmed}{twitchSeparator}t={offsetSeconds}s";
    }

    private static bool IsPlaybackQueryParam(string part)
    {
        return part.StartsWith("t=", StringComparison.OrdinalIgnoreCase)
            || part.StartsWith("start=", StringComparison.OrdinalIgnoreCase)
            || part.StartsWith("time_continue=", StringComparison.OrdinalIgnoreCase);
    }
}
