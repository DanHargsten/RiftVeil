using RiftVeil.Domain.Common;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Domain.Entities;

/// <summary>
/// Represents a tournament within a league.
/// </summary>
public class Tournament : BaseEntity
{
    public int LeagueId { get; private set; }
    public League League { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Stage { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset? EndsAtUtc { get; private set; }
    public TournamentStatus Status { get; private set; } = TournamentStatus.Upcoming;
    public string? ExternalId { get; private set; }
    public string? LiquipediaSlug { get; private set; }

    public ICollection<Match> Matches { get; private set; } = [];


    // Required for EF Core materialization without exposing public setters.
    private Tournament() { }

    public Tournament(
        int leagueId,
        string name,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? endsAtUtc,
        TournamentStatus status = TournamentStatus.Upcoming,
        string? stage = null,
        string? externalId = null,
        string? liquipediaSlug = null)
    {
        LeagueId = leagueId;
        Name = ValidationUtils.ValidateName(name, nameof(name));
        Stage = ValidationUtils.NormalizeOptional(stage);
        StartsAtUtc = ValidationUtils.EnsureUtc(startsAtUtc);
        EndsAtUtc = endsAtUtc is null ? null : ValidationUtils.EnsureUtc(endsAtUtc.Value);
        Status = status;
        ExternalId = ValidationUtils.NormalizeOptional(externalId);
        LiquipediaSlug = ValidationUtils.NormalizeOptional(liquipediaSlug);
    }
}
