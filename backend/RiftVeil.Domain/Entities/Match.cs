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
    
    public int Team1Id { get; private set; }
    public Team Team1 { get; private set; } = null!;
    
    public int Team2Id { get; private set; }
    public Team Team2 { get; private set; } = null!;
    
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? FinishedAtUtc { get; private set; }
    public int BestOf { get; private set; }
    public MatchStatus Status { get; private set; } = MatchStatus.Scheduled;
    public int? Team1Score { get; private set; }
    public int? Team2Score { get; private set; }
    public string? VodUrl { get; private set; }
    public string? ExternalId { get; private set; }
    
    public ICollection<Game> Games { get; private set;  } = [];


    // Required for EF Core materialization without exposing public setters.
    private Match() { }

    public Match(
        int tournamentId,
        int team1Id,
        int team2Id,
        DateTimeOffset startsAtUtc,
        int bestOf,
        MatchStatus status = MatchStatus.Scheduled,
        string? externalId = null,
        string? vodUrl = null)
    {
        if (bestOf <= 0)
            throw new ArgumentOutOfRangeException(nameof(bestOf), "BestOf must be a positive number.");

        if (bestOf is not (1 or 2 or 3 or 5))
            throw new ArgumentOutOfRangeException(nameof(bestOf), "BestOf must be 1, 2, 3, or 5.");
        
        if (team1Id == team2Id)
            throw new ArgumentException("A team cannot play against itself");

        TournamentId = tournamentId;
        Team1Id = team1Id;
        Team2Id = team2Id;
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
