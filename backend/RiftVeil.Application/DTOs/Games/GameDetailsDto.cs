namespace RiftVeil.Application.Dtos.Games;

/// <summary>
/// Full scoreboard-style payload for one game: sides, VOD link, per-player stats, team totals, and draft order.
/// </summary>
public class GameDetailsDto
{
    public int GameId { get; init; }
    public int GameNumber { get; init; }

    /// <summary>
    /// Winning side: 1 or 2; null if the game is unfinished.
    /// </summary>
    public int? WinningTeam { get; init; }
    public string? Team1Side { get; init; }
    public string? Team2Side { get; init; }
    public string? VodUrl { get; init; }

    public List<PlayerStatsDto> Team1Players { get; init; } = [];
    public List<PlayerStatsDto> Team2Players { get; init; } = [];
    public TeamStatsDto? Team1Stats { get; init; }
    public TeamStatsDto? Team2Stats { get; init; }
    public List<DraftEntryDto> Draft { get; init; } = [];
}

/// <summary>
/// One player's line on the game scoreboard (Leaguepedia ScoreboardPlayers shape).
/// </summary>
public class PlayerStatsDto
{
    public string PlayerName { get; init; } = null!;
    public string IngameRole { get; init; } = null!;
    public string Champion { get; init; } = null!;
    public int Kills { get; init; }
    public int Deaths { get; init; }
    public int Assists { get; init; }
    public int GoldEarned { get; init; }
    public int CreepScore { get; init; }
    public int DamageDealtToChampions { get; init; }
    public int VisionScore { get; init; }

    /// <summary>
    /// Comma-separated item IDs as returned by the data source, e.g. "3157,3089,3040".
    /// </summary>
    public string? ItemIds { get; init; }
    public string? TrinketId { get; init; }
    public string? SummonerSpell1Id { get; init; }
    public string? SummonerSpell2Id { get; init; }

    /// <summary>
    /// 1 or 2 — same convention as <see cref="GameDetailsDto.WinningTeam"/>.
    /// </summary>
    public int TeamNumber { get; init; }
}

/// <summary>
/// Aggregated objectives and economy for one team in a game.
/// </summary>
public class TeamStatsDto
{
    public int TotalKills { get; init; }
    public int TotalGoldEarned { get; init; }
    public int TowersDestroyed { get; init; }
    public int InhibitorsDestroyed { get; init; }
    public int BaronsSlain { get; init; }
    public int RiftHeraldsSlain { get; init; }
    public int VoidGrubsSlain { get; init; }
    public int TotalDragonsSlain { get; init; }
    public int InfernalDragonsSlain { get; init; }
    public int MountainDragonsSlain { get; init; }
    public int CloudDragonsSlain { get; init; }
    public int OceanDragonsSlain { get; init; }
    public int HextechDragonsSlain { get; init; }
    public int ChemtechDragonsSlain { get; init; }
    public int ElderDragonsSlain { get; init; }
    public int GameDurationSeconds { get; init; }

    /// <summary>
    /// 1 or 2 — same convention as <see cref="GameDetailsDto.WinningTeam"/>.
    /// </summary>
    public int TeamNumber { get; init; }
}

/// <summary>
/// One pick or ban step in chronological order for the game's draft.
/// </summary>
public class DraftEntryDto
{
    /// <summary>
    /// 1 or 2 — same convention as <see cref="GameDetailsDto.WinningTeam"/>.
    /// </summary>
    public int TeamNumber { get; init; }
    public string Phase { get; init; } = null!;
    public int SequenceNumber { get; init; }
    public string Champion { get; init; } = null!;
}
