using RiftVeil.Domain.Common;

namespace RiftVeil.Domain.Entities;

/// <summary>
/// Team-level objective stats for one team in one game.
/// Two rows per game - one for each team.
/// Sourced from Leaguepedia's ScoreboardTeams table.
/// </summary>
public class GameTeamStats : BaseEntity
{
    public int GameId { get; private set; }
    public Game Game { get; private set; } = null!;
    
    /// <summary>
    /// 1 or 2 - matches Game.WinningTeam convention.
    /// </summary>
    public int TeamNumber { get; private set; }
    
    public int TotalKills { get; private set; }
    public int TotalDeaths { get; private set; }
    public int TotalAssists { get; private set; }
    public int TotalGoldEarned { get; private set; }
    public int TowersDestroyed { get; private set; }
    public int InhibitorsDestroyed { get; private set; }
    public int BaronsSlain { get; private set; }
    public int RiftHeraldsSlain { get; private set; }
    public int VoidGrubsSlain { get; private set; }
    
    /// <summary>
    /// Dragon counts - split by type to allow richer display.
    /// </summary>
    public int TotalDragonsSlain { get; private set; }
    public int InfernalDragonsSlain { get; private set; }
    public int MountainDragonsSlain { get; private set; }
    public int CloudDragonsSlain { get; private set; }
    public int OceanDragonsSlain { get; private set; }
    public int HextechDragonsSlain { get; private set; }
    public int ChemtechDragonsSlain { get; private set; }
    public int ElderDragonsSlain { get; private set; }
    
    /// <summary>
    /// Game length in seconds. Stored on both rows for convenience.
    /// </summary>
    public int GameDurationSeconds { get; private set; }
    
    
    // Required for EF Core materialization without exposing public setters.
    private GameTeamStats() { }

    public GameTeamStats(
        int gameId,
        int teamNumber,
        int totalKills,
        int totalDeaths,
        int totalAssists,
        int totalGoldEarned,
        int towersDestroyed,
        int inhibitorsDestroyed,
        int baronsSlain,
        int riftHeraldsSlain,
        int voidGrubsSlain,
        int totalDragonsSlain,
        int infernalDragonsSlain,
        int mountainDragonsSlain,
        int cloudDragonsSlain,
        int oceanDragonsSlain,
        int hextechDragonsSlain,
        int chemtechDragonsSlain,
        int elderDragonsSlain,
        int gameDurationSeconds)
    {
        if (teamNumber is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(teamNumber), "Team number must be 1 or 2.");
        
        GameId = gameId;
        TeamNumber = teamNumber;
        TotalKills = totalKills;
        TotalDeaths = totalDeaths;
        TotalAssists = totalAssists;
        TotalGoldEarned = totalGoldEarned;
        TowersDestroyed = towersDestroyed;
        InhibitorsDestroyed = inhibitorsDestroyed;
        BaronsSlain = baronsSlain;
        RiftHeraldsSlain = riftHeraldsSlain;
        VoidGrubsSlain = voidGrubsSlain;
        TotalDragonsSlain = totalDragonsSlain;
        InfernalDragonsSlain = infernalDragonsSlain;
        MountainDragonsSlain = mountainDragonsSlain;
        CloudDragonsSlain = cloudDragonsSlain;
        OceanDragonsSlain = oceanDragonsSlain;
        HextechDragonsSlain = hextechDragonsSlain;
        ChemtechDragonsSlain = chemtechDragonsSlain;
        ElderDragonsSlain = elderDragonsSlain;
        GameDurationSeconds = gameDurationSeconds;
    }
}
