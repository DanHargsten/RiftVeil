using Microsoft.EntityFrameworkCore;
using RiftVeil.Application.Dtos.Games;
using RiftVeil.Application.Interfaces.Read;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Read;

/// <summary>
/// Read-side queries for a single game's player stats, team stats, and draft.
/// </summary>
public class GameReadService(RiftVeilDbContext dbContext) : IGameReadService
{
    /// <inheritdoc />
    public async Task<GameDetailsDto?> GetDetailsByIdAsync(int gameId)
    {
        var snapshot = await dbContext.Games
            .Where(game => game.Id == gameId)
            .Select(game => new
            {
                game.Id,
                game.GameNumber,
                game.WinningTeam,
                game.Team1Side,
                game.Team2Side,
                game.VodUrl,
                PlayerStats = game.PlayerStats.Select(playerStat => new PlayerStatsDto
                {
                    PlayerName = playerStat.PlayerName,
                    IngameRole = playerStat.IngameRole,
                    Champion = playerStat.Champion,
                    Kills = playerStat.Kills,
                    Deaths = playerStat.Deaths,
                    Assists = playerStat.Assists,
                    GoldEarned = playerStat.GoldEarned,
                    CreepScore = playerStat.CreepScore,
                    DamageDealtToChampions = playerStat.DamageDealtToChampions,
                    VisionScore = playerStat.VisionScore,
                    ItemIds = playerStat.ItemIds,
                    TrinketId = playerStat.TrinketId,
                    SummonerSpell1Id = playerStat.SummonerSpell1Id,
                    SummonerSpell2Id = playerStat.SummonerSpell2Id,
                    TeamNumber = playerStat.TeamNumber,
                }).ToList(),
                TeamStats = game.TeamStats.Select(teamStat => new TeamStatsDto
                {
                    TotalKills = teamStat.TotalKills,
                    TotalDeaths = teamStat.TotalDeaths,
                    TotalAssists = teamStat.TotalAssists,
                    TotalGoldEarned = teamStat.TotalGoldEarned,
                    TowersDestroyed = teamStat.TowersDestroyed,
                    InhibitorsDestroyed = teamStat.InhibitorsDestroyed,
                    BaronsSlain = teamStat.BaronsSlain,
                    RiftHeraldsSlain = teamStat.RiftHeraldsSlain,
                    VoidGrubsSlain = teamStat.VoidGrubsSlain,
                    TotalDragonsSlain = teamStat.TotalDragonsSlain,
                    InfernalDragonsSlain = teamStat.InfernalDragonsSlain,
                    MountainDragonsSlain = teamStat.MountainDragonsSlain,
                    CloudDragonsSlain = teamStat.CloudDragonsSlain,
                    OceanDragonsSlain = teamStat.OceanDragonsSlain,
                    HextechDragonsSlain = teamStat.HextechDragonsSlain,
                    ChemtechDragonsSlain = teamStat.ChemtechDragonsSlain,
                    ElderDragonsSlain = teamStat.ElderDragonsSlain,
                    GameDurationSeconds = teamStat.GameDurationSeconds,
                    TeamNumber = teamStat.TeamNumber,
                }).ToList(),
                DraftEntries = game.DraftEntries
                    .OrderBy(draftEntry => draftEntry.SequenceNumber)
                    .Select(draftEntry => new DraftEntryDto
                    {
                        TeamNumber = draftEntry.TeamNumber,
                        Phase = draftEntry.Phase,
                        SequenceNumber = draftEntry.SequenceNumber,
                        Champion = draftEntry.Champion,
                    }).ToList(),
            })
            .FirstOrDefaultAsync();

        if (snapshot == null) return null;

        return new GameDetailsDto
        {
            GameId = snapshot.Id,
            GameNumber = snapshot.GameNumber,
            WinningTeam = snapshot.WinningTeam,
            Team1Side = snapshot.Team1Side,
            Team2Side = snapshot.Team2Side,
            VodUrl = snapshot.VodUrl,
            Team1Players = snapshot.PlayerStats
                .Where(playerStat => playerStat.TeamNumber == 1)
                .OrderBy(playerStat => RoleOrder(playerStat.IngameRole))
                .ToList(),
            Team2Players = snapshot.PlayerStats
                .Where(playerStat => playerStat.TeamNumber == 2)
                .OrderBy(playerStat => RoleOrder(playerStat.IngameRole))
                .ToList(),
            Team1Stats = snapshot.TeamStats.FirstOrDefault(teamStat => teamStat.TeamNumber == 1),
            Team2Stats = snapshot.TeamStats.FirstOrDefault(teamStat => teamStat.TeamNumber == 2),
            Draft = snapshot.DraftEntries,
        };
    }

    /// <summary>
    /// Stable lane order for display (top → support).
    /// </summary>
    private static int RoleOrder(string role) => role.ToLowerInvariant() switch
    {
        "top" => 0,
        "jungle" => 1,
        "mid" => 2,
        "bot" or "adc" => 3,
        "support" => 4,
        _ => 5,
    };
}
