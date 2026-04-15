using RiftVeil.Domain.Common;

namespace RiftVeil.Domain.Entities;

/// <summary>
/// Scoreboard stats for one player in one game.
/// One row per player per game - ten rows total per game in a standard match.
/// Sourced from Leaguepedia's ScoreboardPlayers table.
/// </summary>
public class GamePlayerStats : BaseEntity
{
    public int GameId { get; private set; }
    public Game Game { get; private set; } = null!;
    
    /// <summary>
    /// 1 or 2 - matches Game.WinningTeam convention.
    /// </summary>
    public int TeamNumber { get; private set; }

    /// <summary>
    /// Player's in-game name as stored on Leaguepedia.
    /// </summary>
    public string PlayerName { get; private set; } = null!;

    /// <summary>
    /// Lane role: Top, Jungle, Mid, Bot, Support.
    /// </summary>
    public string Role { get; private set; } = null!;

    public string Champion { get; private set; } = null!;
    
    public int Kills { get; private set; }
    public int Deaths { get; private set; }
    public int Assists { get; private set; }
    
    /// <summary>
    /// Total gold earned during the game.
    /// </summary>
    public int GoldEarned { get; private set; }
    
    /// <summary>
    /// Minions + neutral monsters killed.
    /// </summary>
    public int CreepScore { get; private set; }
    
    public int DamageDealtToChampions { get; private set; }
    public int VisionScore { get; private set; }
    
    /// <summary>
    /// Comma-separated item IDs as returned by Leaguepedia, e.g. "3157,3089,3040".
    /// Stored as a string to avoid a separate join table for a display-only list.
    /// </summary>
    public string? ItemIds { get; private set; }
    
    /// <summary>
    /// Trinket/Ward item ID.
    /// </summary>
    public string? TrinketId { get; private set; }
    
    public string? SummonerSpell1Id { get; private set; }
    public string? SummonerSpell2Id { get; private set; }
    
    /// <summary>
    /// Required for EF Core materialization without exposing public setters.
    /// </summary>
    private GamePlayerStats() { }

    public GamePlayerStats(
        int gameId,
        int teamNumber,
        string playerName,
        string role,
        string champion,
        int kills,
        int deaths,
        int assists,
        int goldEarned,
        int creepScore,
        int damageDealtToChampions,
        int visionScore,
        string? itemIds = null,
        string? trinketId = null,
        string? summonerSpell1Id = null,
        string? summonerSpell2Id = null)
    {
        if (teamNumber is not (1 or 2))
            throw new ArgumentOutOfRangeException(nameof(teamNumber), "Team number must be 1 or 2.");
        
        if (string.IsNullOrWhiteSpace(playerName))
            throw new ArgumentException("Player name is required.", nameof(playerName));
        
        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role is required.", nameof(role));
        
        if (string.IsNullOrWhiteSpace(champion))
            throw new ArgumentException("Champion is required.", nameof(champion));
        
        GameId = gameId;
        TeamNumber = teamNumber;
        PlayerName = playerName.Trim();
        Role = role.Trim();
        Champion = champion.Trim();
        Kills = kills;
        Deaths = deaths;
        Assists = assists;
        GoldEarned = goldEarned;
        CreepScore = creepScore;
        DamageDealtToChampions = damageDealtToChampions;
        VisionScore = visionScore;
        ItemIds = string.IsNullOrWhiteSpace(itemIds) ? null : itemIds.Trim();
        TrinketId = string.IsNullOrWhiteSpace(trinketId) ? null : trinketId.Trim();
        SummonerSpell1Id = string.IsNullOrWhiteSpace(summonerSpell1Id) ? null : summonerSpell1Id.Trim();
        SummonerSpell2Id = string.IsNullOrWhiteSpace(summonerSpell2Id) ? null : summonerSpell2Id.Trim();
    }
}
