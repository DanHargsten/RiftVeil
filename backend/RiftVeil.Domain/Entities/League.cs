using RiftVeil.Domain.Common;

namespace RiftVeil.Domain.Entities;

/// <summary>
/// Represents a competitive league that owns tournaments.
/// </summary>
public class League : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string ShortName { get; private set; } = null!;
    public string? Region { get; private set; }
    public string? LogoUrl { get; private set; }
    public string? ExternalId { get; private set; }

    public ICollection<Tournament> Tournaments { get; private set; } = [];


    // Required for EF Core materialization without exposing public setters.
    private League() { }

    public League(string name, string shortName, string? region = null, string? logoUrl = null, string? externalId = null)
    {
        Name = ValidationUtils.ValidateName(name, nameof(name));
        ShortName = ValidationUtils.ValidateShortName(shortName, nameof(shortName));
        Region = ValidationUtils.NormalizeOptional(region);
        LogoUrl = ValidationUtils.NormalizeOptional(logoUrl);
        ExternalId = ValidationUtils.NormalizeOptional(externalId);
    }
}
