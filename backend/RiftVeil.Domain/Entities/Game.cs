using RiftVeil.Domain.Common;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Domain.Entities;

/// <summary>
/// Represents an individual game within a match (e.g., Game 1 of a Bo3).
/// </summary>
public class Game : BaseEntity
{
    public int MatchId { get; private set; }
    public Match Match { get; private set; } = null!;
    public int GameNumber { get; private set; }  // 1, 2, 3, 4, 5
    public string? Team1Side { get; private set; }  // "Blue" or "Red"
    public string? Team2Side { get; private set; }  // "Blue" or "Red"
    public int? WinningTeam { get; private set; }  // 1 or 2 (null if not finished)
    public TimeSpan? Duration { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? FinishedAtUtc { get; private set; }
    public string? VodUrl { get; private set; }
    public string? ExternalId { get; private set; }

    public ICollection<GameVod> Vods { get; private set; } = [];
    public ICollection<GamePlayerStats> PlayerStats { get; private set; } = [];
    public ICollection<GameTeamStats> TeamStats { get; private set; } = [];
    public ICollection<GameDraftEntry> DraftEntries { get; private set; } = [];


    // Required for EF Core materialization without exposing public setters.
    private Game() { }

    public Game(
        int matchId,
        int gameNumber,
        string? team1Side = null,
        string? team2Side = null,
        int? winningTeam = null,
        TimeSpan? duration = null,
        DateTimeOffset? startedAtUtc = null,
        DateTimeOffset? finishedAtUtc = null,
        string? vodUrl = null,
        string? externalId = null)
    {
        if (gameNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gameNumber), "Game number must be positive.");
        }

        // Soft validation: warn about unusual game numbers but allow them
        if (gameNumber > 5)
        {
            // TODO: Add logging when logger is available
            // This allows future formats (Bo7, Bo9) or special tournaments
            // but flags potentially incorrect data
        }

        if (winningTeam.HasValue && winningTeam.Value is not (1 or 2))
        {
            throw new ArgumentOutOfRangeException(nameof(winningTeam), "Winning team must be 1 or 2.");
        }

        // Validate sides if provided
        ValidateSide(team1Side, nameof(team1Side));
        ValidateSide(team2Side, nameof(team2Side));

        // Ensure sides are different if both are specified
        if (team1Side != null && team2Side != null && team1Side == team2Side)
        {
            throw new ArgumentException("Team1Side and Team2Side must be different.", nameof(team2Side));
        }

        MatchId = matchId;
        GameNumber = gameNumber;
        Team1Side = NormalizeSide(team1Side);
        Team2Side = NormalizeSide(team2Side);
        WinningTeam = winningTeam;
        Duration = duration;
        StartedAtUtc = startedAtUtc.HasValue ? ValidationUtils.EnsureUtc(startedAtUtc.Value) : null;
        FinishedAtUtc = finishedAtUtc.HasValue ? ValidationUtils.EnsureUtc(finishedAtUtc.Value) : null;
        VodUrl = ValidationUtils.NormalizeOptional(vodUrl);
        ExternalId = ValidationUtils.NormalizeOptional(externalId);
    }

    private static void ValidateSide(string? side, string paramName)
    {
        if (side == null)
        {
            return;
        }

        var normalized = side.Trim().ToUpperInvariant();
        if (normalized is not ("BLUE" or "RED"))
        {
            throw new ArgumentException("Side must be 'Blue' or 'Red'.", paramName);
        }
    }

    private static string? NormalizeSide(string? side)
    {
        if (string.IsNullOrWhiteSpace(side))
        {
            return null;
        }

        var normalized = side.Trim().ToUpperInvariant();
        return normalized switch
        {
            "BLUE" => "Blue",
            "RED" => "Red",
            _ => null
        };
    }

    /// <summary>
    /// Sets only the winning team (used when updating existing games with missing result).
    /// </summary>
    public void SetWinningTeam(int winningTeam)
    {
        if (winningTeam is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(winningTeam), "Winning team must be 1 or 2.");
        WinningTeam = winningTeam;
    }

    /// <summary>
    /// Marks the game as finished with a winner.
    /// </summary>
    public void MarkFinished(
        int winningTeam,
        DateTimeOffset startedAtUtc,
        DateTimeOffset finishedAtUtc,
        string? vodUrl = null)
    {
        if (winningTeam is not (1 or 2))
        {
            throw new ArgumentOutOfRangeException(nameof(winningTeam), "Winning team must be 1 or 2.");
        }

        StartedAtUtc = ValidationUtils.EnsureUtc(startedAtUtc);
        FinishedAtUtc = ValidationUtils.EnsureUtc(finishedAtUtc);

        if (finishedAtUtc < startedAtUtc)
        {
            throw new ArgumentException("FinishedAtUtc cannot be earlier than StartedAtUtc", nameof(finishedAtUtc));
        }

        Duration = finishedAtUtc - startedAtUtc;
        WinningTeam = winningTeam;
        VodUrl = ValidationUtils.NormalizeOptional(vodUrl);
    }

    /// <summary>
    /// Used by the VOD enricher to set the YouTube link.
    /// </summary>
    /// <param name="vodUrl">Primary VOD URL for quick access; null clears the value.</param>
    public void SetVodUrl(string? vodUrl)
    {
        VodUrl = vodUrl;
    }

    public GameVod? AddGameVod(
        VodProvider provider,
        string url,
        string? locale = null,
        string? parameter = null,
        int offsetSeconds = 0,
        int priority = 0)
    {
        var normalizedLocale = ValidationUtils.NormalizeOptional(locale);

        if (Vods.Any(v => v.Provider == provider && v.Locale == normalizedLocale))
        {
            return null;
        }

        var vod = new GameVod(Id, provider, url, normalizedLocale, parameter, offsetSeconds, priority);
        Vods.Add(vod);
        return vod;
    }
}
