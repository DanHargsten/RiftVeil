namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Builds stable image URLs from Leaguepedia Cargo file fields.
/// </summary>
public static class LeaguepediaImageUrls
{
    private const string FilePathBase = "https://lol.fandom.com/wiki/Special:FilePath/";

    /// <summary>
    /// Converts a Cargo <c>Teams.Image</c> filename to a Fandom CDN URL via Special:FilePath.
    /// </summary>
    public static string? TeamLogoFromCargoImage(string? imageFileName)
    {
        if (string.IsNullOrWhiteSpace(imageFileName))
            return null;

        var trimmed = imageFileName.Trim();
        return FilePathBase + Uri.EscapeDataString(trimmed);
    }

    /// <summary>
    /// Leaguepedia icon-only asset (Cargo often stores the wordmark in <c>Image</c>).
    /// Naming convention: <c>{Team}logo square.png</c> vs <c>{Team}logo std.png</c>.
    /// </summary>
    public static string? TeamMarkFromCargoImage(string? imageFileName)
    {
        var squareFile = ToSquareLogoFileName(imageFileName);
        return squareFile == null ? null : TeamLogoFromCargoImage(squareFile);
    }

    /// <summary>
    /// Maps wordmark filenames to the square/icon variant when one exists on Leaguepedia.
    /// </summary>
    public static string? ToSquareLogoFileName(string? imageFileName)
    {
        if (string.IsNullOrWhiteSpace(imageFileName))
            return null;

        var trimmed = imageFileName.Trim();

        if (trimmed.Contains("logo square", StringComparison.OrdinalIgnoreCase))
            return trimmed;

        if (LogoVariantRegex().IsMatch(trimmed))
            return LogoVariantRegex().Replace(trimmed, "logo square.png", 1);

        if (LogoBareRegex().IsMatch(trimmed))
            return LogoBareRegex().Replace(trimmed, "logo square.png", 1);

        return null;
    }

    /// <summary>
    /// Derives the icon URL from a stored <see cref="Team.LogoUrl"/> FilePath link.
    /// </summary>
    public static string? TeamMarkFromLogoUrl(string? logoUrl)
    {
        var fileName = ExtractFileNameFromFilePathUrl(logoUrl);
        return fileName == null ? null : TeamMarkFromCargoImage(fileName);
    }

    private static string? ExtractFileNameFromFilePathUrl(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
            return null;

        const string marker = "/Special:FilePath/";
        var index = logoUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var encoded = logoUrl[(index + marker.Length)..];
        var query = encoded.IndexOf('?', StringComparison.Ordinal);
        if (query >= 0)
            encoded = encoded[..query];

        return string.IsNullOrWhiteSpace(encoded) ? null : Uri.UnescapeDataString(encoded);
    }

    /// <summary>Matches <c>logo profile.png</c>, <c>logo std.png</c>, etc.</summary>
    private static System.Text.RegularExpressions.Regex LogoVariantRegex() =>
        new(@"logo\s+(?!square)\S+\.png$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static System.Text.RegularExpressions.Regex LogoBareRegex() =>
        new(@"logo\.png$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
