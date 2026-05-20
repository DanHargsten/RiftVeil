using RiftVeil.Infrastructure.Services.Import;

namespace RiftVeil.Api.Tests.Services;

public class LeaguepediaImageUrlsTests
{
    [Fact]
    public void TeamLogoFromCargoImage_ReturnsNull_WhenEmpty()
    {
        Assert.Null(LeaguepediaImageUrls.TeamLogoFromCargoImage(null));
        Assert.Null(LeaguepediaImageUrls.TeamLogoFromCargoImage("   "));
    }

    [Fact]
    public void TeamLogoFromCargoImage_BuildsFilePathUrl()
    {
        var url = LeaguepediaImageUrls.TeamLogoFromCargoImage("100 Thieveslogo profile.png");

        Assert.Equal(
            "https://lol.fandom.com/wiki/Special:FilePath/100%20Thieveslogo%20profile.png",
            url);
    }

    [Theory]
    [InlineData("LYONlogo std.png", "LYONlogo square.png")]
    [InlineData("FlyQuestlogo profile.png", "FlyQuestlogo square.png")]
    [InlineData("Invictus Gaminglogo profile.png", "Invictus Gaminglogo square.png")]
    [InlineData("T1logo.png", "T1logo square.png")]
    [InlineData("Gen.Glogo square.png", "Gen.Glogo square.png")]
    public void ToSquareLogoFileName_MapsWordmarkToSquare(string input, string expected)
    {
        Assert.Equal(expected, LeaguepediaImageUrls.ToSquareLogoFileName(input));
    }

    [Fact]
    public void TeamMarkFromLogoUrl_TransformsProfileFilePath()
    {
        var url = LeaguepediaImageUrls.TeamMarkFromLogoUrl(
            "https://lol.fandom.com/wiki/Special:FilePath/FlyQuestlogo%20profile.png");

        Assert.Equal(
            "https://lol.fandom.com/wiki/Special:FilePath/FlyQuestlogo%20square.png",
            url);
    }
}
