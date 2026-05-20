namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Resolves square/icon FilePath URLs from Cargo <c>Teams.Image</c> and verifies they exist on Fandom.
/// </summary>
public class LeaguepediaTeamLogoVerifier(LeaguepediaClient leaguepediaClient)
{
    public async Task<string?> ResolveVerifiedIconUrlAsync(string? imageFileName, CancellationToken cancellationToken = default)
    {
        var squareFile = LeaguepediaImageUrls.ToSquareLogoFileName(imageFileName);
        if (squareFile == null)
            return null;

        var squareUrl = LeaguepediaImageUrls.TeamLogoFromCargoImage(squareFile);
        if (squareUrl == null)
            return null;

        if (await leaguepediaClient.FilePathUrlExistsAsync(squareUrl, cancellationToken))
            return squareUrl;

        // Cargo naming is reliable; store derived URL when HEAD fails (rate limits, etc.).
        return squareUrl;
    }
}
