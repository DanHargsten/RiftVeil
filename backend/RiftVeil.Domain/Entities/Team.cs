using RiftVeil.Domain.Common;

namespace RiftVeil.Domain.Entities;

/// <summary>
/// Represents a team that participates in matches.
/// </summary>
public class Team : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string ShortName { get; private set; } = null!;
    public string? Region { get; private set; }
    public string? LogoUrl { get; private set; }
    /// <summary>
    /// Icon-only logo (e.g. Leaguepedia <c>logo square.png</c> or LoL Esports isotype).
    /// </summary>
    public string? IconLogoUrl { get; private set; }
    public string? ExternalId { get; private set; }

    public ICollection<Match> MatchesAsTeam1 { get; private set; } = [];
    public ICollection<Match> MatchesAsTeam2 { get; private set; } = [];

    private Team() { }

    public Team(
        string name,
        string shortName,
        string? region = null,
        string? logoUrl = null,
        string? iconLogoUrl = null,
        string? externalId = null)
    {
        Name = ValidationUtils.ValidateName(name, nameof(name));
        ShortName = ValidationUtils.ValidateShortName(shortName, nameof(shortName));
        Region = ValidationUtils.NormalizeOptional(region);
        LogoUrl = ValidationUtils.NormalizeOptional(logoUrl);
        IconLogoUrl = ValidationUtils.NormalizeOptional(iconLogoUrl);
        ExternalId = ValidationUtils.NormalizeOptional(externalId);
    }

    /// <summary>
    /// Sets the team logo URL (e.g. from Leaguepedia import). Pass null to clear.
    /// </summary>
    public void SetLogoUrl(string? logoUrl)
    {
        LogoUrl = ValidationUtils.NormalizeOptional(logoUrl);
    }

    public void SetIconLogoUrl(string? iconLogoUrl)
    {
        IconLogoUrl = ValidationUtils.NormalizeOptional(iconLogoUrl);
    }

    public void SetRegion(string? region)
    {
        Region = ValidationUtils.NormalizeOptional(region);
    }

    /// <summary>
    /// Leaguepedia overview page slug (Cargo <c>Teams.OverviewPage</c>).
    /// </summary>
    public void SetExternalId(string? externalId)
    {
        ExternalId = ValidationUtils.NormalizeOptional(externalId);
    }

    public void SetShortName(string shortName)
    {
        ShortName = ValidationUtils.ValidateShortName(shortName, nameof(shortName));
    }

    public void SetName(string name)
    {
        Name = ValidationUtils.ValidateName(name, nameof(name));
    }
}
