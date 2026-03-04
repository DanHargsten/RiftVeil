using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Application.Mappings;

/// <summary>
/// Centralized logic for selecting the best VOD from a collection.
/// Used by projections and the enricher to ensure consistent priority.
/// </summary>
public static class VodSelectors
{
    /// <summary>
    /// Selects the best VOD URL from a collection of GameVods.
    /// Priority: lowest Priority value -> preferred locale match -> YouTube VODs first
    /// </summary>
    /// <param name="vods"></param>
    /// <param name="preferredLocale"></param>
    /// <returns></returns>
    public static string? GetBestVodUrl(IEnumerable<GameVod> vods, string? preferredLocale = null){
        return vods
            .OrderBy(v => v.Priority)
            .ThenBy(v => preferredLocale != null && v.Locale != preferredLocale ? 1 : 0)
            .ThenBy(v => v.Provider == VodProvider.YouTube ? 0 : 1)
            .Select(v => v.Url)
            .FirstOrDefault();
    }
}