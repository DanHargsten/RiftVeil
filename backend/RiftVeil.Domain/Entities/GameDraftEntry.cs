using RiftVeil.Domain.Common;

namespace RiftVeil.Domain.Entities;

/// <summary>
/// One pick and ban in a game's draft phase.
/// Sourced from Leaguepedia's PickAndBansS7 table.
///
/// Standard draft order (Bo1, phase 1 = bans 1-6, phase 2 = picks 1-6, phase 3 = bans 7-10, phase 4 = picks 7-10):
/// SequenceNumber 1-20 maps to the full draft in chronological order.
/// </summary>
public class GameDraftEntry : BaseEntity
{
    public int GameId { get; private set; }
    public Game Game { get; private set; } = null!;
    
    /// <summary>
    /// 1 or 2 - matches Game.WinningTeam convention.
    /// </summary>
    public int TeamNumber { get; private set; }

    /// <summary>
    /// "Pick" or "Ban"."
    /// </summary>
    public string Phase { get; private set; } = null!;
    
    /// <summary>
    /// Chronological position in the draft (1-20).
    /// Allows the frontend to reconstruct the exact draft order.
    /// </summary>
    public int SequenceNumber { get; private set; }

    public string Champion { get; private set; } = null!;
    
    // Required for EF Core materialization without exposing public setters.
    private GameDraftEntry() { }

    public GameDraftEntry(
        int gameId,
        int teamNumber,
        string phase,
        int sequenceNumber,
        string champion)
    {
        if (teamNumber is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(teamNumber), "Team number must be 1 or 2.");
        
        if (phase is not ("Pick" or "Ban"))
            throw new ArgumentException("Phase must be 'Pick' or 'Ban'.", nameof(phase));
        
        if (sequenceNumber <= 0)
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber), "Sequence number must be positive.");
        
        if (string.IsNullOrWhiteSpace(champion))
            throw new ArgumentException("Champion is required.", nameof(champion));
        
        GameId = gameId;
        TeamNumber = teamNumber;
        Phase = phase;
        SequenceNumber = sequenceNumber;
        Champion = champion.Trim();
    }
}
