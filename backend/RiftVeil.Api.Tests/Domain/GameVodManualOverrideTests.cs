using RiftVeil.Application.Mappings;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Api.Tests.Domain;

public class GameVodManualOverrideTests
{
    [Fact]
    public void ApplyManualVod_KeepsImportedVodAndSetsManualAsPrimary()
    {
        var game = new Game(matchId: 1, gameNumber: 1);
        var imported = game.AddGameVod(
            VodProvider.YouTube,
            "https://www.youtube.com/watch?v=importedA&t=55s",
            locale: "en-US",
            parameter: "importedA",
            offsetSeconds: 55,
            source: VodSource.Imported);

        var manual = game.ApplyManualVod(
            VodProvider.YouTube,
            "https://www.youtube.com/watch?v=manualB&t=11s",
            parameter: "manualB",
            draftOffsetSeconds: 10471,
            gameStartOffsetSeconds: 8121);

        Assert.NotNull(imported);
        Assert.NotNull(manual);
        Assert.Equal(2, game.Vods.Count);
        Assert.Contains(game.Vods, vod => vod.Source == VodSource.Imported && vod.Locale == "en-US");
        Assert.Contains(game.Vods, vod => vod.Source == VodSource.Manual && vod.Locale == null);
        Assert.Equal("https://www.youtube.com/watch?v=manualB&t=8121s", game.VodUrl);
    }

    [Fact]
    public void AddGameVod_AllowsManualAndImportedForSameProviderAndLocale()
    {
        var game = new Game(matchId: 1, gameNumber: 1);

        var imported = game.AddGameVod(
            VodProvider.YouTube,
            "https://www.youtube.com/watch?v=importedA",
            locale: "en-US",
            source: VodSource.Imported);

        var manual = game.AddGameVod(
            VodProvider.YouTube,
            "https://www.youtube.com/watch?v=manualB",
            locale: "en-US",
            source: VodSource.Manual);

        var duplicateImported = game.AddGameVod(
            VodProvider.YouTube,
            "https://www.youtube.com/watch?v=importedC",
            locale: "en-US",
            source: VodSource.Imported);

        Assert.NotNull(imported);
        Assert.NotNull(manual);
        Assert.Null(duplicateImported);
        Assert.Equal(2, game.Vods.Count);
    }

    [Fact]
    public void RemoveManualVods_ThenSelectorFallsBackToImportedVod()
    {
        var game = new Game(matchId: 1, gameNumber: 1);
        game.AddGameVod(
            VodProvider.Twitch,
            "https://www.twitch.tv/videos/123456",
            locale: "en-US",
            source: VodSource.Imported);
        game.AddGameVod(
            VodProvider.YouTube,
            "https://www.youtube.com/watch?v=importedA",
            locale: "en-US",
            source: VodSource.Imported);
        game.ApplyManualVod(
            VodProvider.YouTube,
            "https://www.youtube.com/watch?v=manualB",
            parameter: "manualB",
            draftOffsetSeconds: 300,
            gameStartOffsetSeconds: 600);

        var removed = game.RemoveManualVods();
        game.SetVodUrl(VodSelectors.GetBestVodUrl(game.Vods, preferredLocale: "en-US"));

        Assert.True(removed);
        Assert.DoesNotContain(game.Vods, vod => vod.Source == VodSource.Manual);
        Assert.Equal("https://www.youtube.com/watch?v=importedA", game.VodUrl);
    }
}
