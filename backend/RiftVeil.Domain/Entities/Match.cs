using RiftVeil.Domain.Common;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Domain.Entities;

/// <summary>
/// Represents a scheduled or completed match within a tournament.
/// </summary>
public class Match : BaseEntity
{
    public int TournamentId { get; private set; }
    public Tournament Tournament { get; private set; } = null!;
    public string Team1Name { get; private set; } = null!;  // Kept only names until Team entity exists.
    public string Team2Name { get; private set; } = null!;  // Kept only names until Team entity exists.
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? FinishedAtUtc { get; private set; }
    public int BestOf { get; private set; }
    public MatchStatus Status { get; private set; } = MatchStatus.Scheduled;
    public int? Team1Score { get; private set; }
    public int? Team2Score { get; private set; }
    public string? VodUrl { get; private set; }
    public string? ExternalId { get; private set; }


    // Required for EF Core materialization without exposing public setters.
    private Match() { }

    public Match(int tournamentId, string team1Name, string team2Name, DateTimeOffset startsAtUtc, int bestOf,
        MatchStatus status = MatchStatus.Scheduled, string? externalId = null, string? vodUrl = null)
    {
        if (bestOf <= 0)
            throw new ArgumentOutOfRangeException(nameof(bestOf), "BestOf must be a positive number.");

        if (bestOf is not (1 or 2 or 3 or 5))
            throw new ArgumentOutOfRangeException(nameof(bestOf), "BestOf must be 1, 2, 3, or 5.");

        TournamentId = tournamentId;
        Team1Name = ValidationUtils.ValidateName(team1Name, nameof(team1Name));
        Team2Name = ValidationUtils.ValidateName(team2Name, nameof(team2Name));
        StartsAtUtc = ValidationUtils.EnsureUtc(startsAtUtc);
        BestOf = bestOf;
        Status = status;
        ExternalId = ValidationUtils.NormalizeOptional(externalId);
        VodUrl = ValidationUtils.NormalizeOptional(vodUrl);
    }

    /// <summary>
    /// Marks the match as live.
    /// </summary>
    /// <param name="startedAtUtc">The UTC time when the match started.</param>
    public void MarkLive(DateTimeOffset startedAtUtc)
    {
        StartedAtUtc = ValidationUtils.EnsureUtc(startedAtUtc);
        Status = MatchStatus.Live;
    }

    /// <summary>
    /// Marks the match as finished.
    /// </summary>
    /// <param name="startedAtUtc">The UTC time when the match started.</param>
    /// <param name="finishedAtUtc">The UTC time when the match finished.</param>
    /// <param name="team1Score">The score of team 1.</param>
    /// <param name="team2Score">The score of team 2.</param>
    /// <param name="vodUrl">The URL of the video on demand.</param>
    public void MarkFinished(DateTimeOffset startedAtUtc, DateTimeOffset finishedAtUtc, int team1Score, int team2Score, string? vodUrl = null)
    {
        StartedAtUtc = ValidationUtils.EnsureUtc(startedAtUtc);
        FinishedAtUtc = ValidationUtils.EnsureUtc(finishedAtUtc);

        if (finishedAtUtc < startedAtUtc)
            throw new ArgumentException("FinishedAtUtc cannot be earlier than StartedAtUtc", nameof(finishedAtUtc));

        Team1Score = team1Score;
        Team2Score = team2Score;

        VodUrl = ValidationUtils.NormalizeOptional(vodUrl);
        Status = MatchStatus.Finished;
    }
}
