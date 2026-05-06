using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RiftVeil.Domain.Entities;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Imports per-game detail stats from Leaguepedia into GamePlayerStats,
/// GameTeamStats, and GameDraftEntry tables.
/// Triggered per tournament via ImportController.
/// </summary>
public class GameDetailImportService(
    LeaguepediaClient client,
    RiftVeilDbContext dbContext,
    IOptions<LeaguepediaClientOptions> leaguepediaOptions)
{
    private readonly LeaguepediaClientOptions _leaguepediaOptions = leaguepediaOptions.Value;
    private class ImportStats
    {
        public int Imported { get; set; }
        public int Skipped { get; set; }
        public int AlreadyExists { get; set; }

        public void Print(string type)
        {
            Console.WriteLine($"\n--- {type} Import Summary ---");
            Console.WriteLine($"{Imported} imported");
            Console.WriteLine($"{Skipped} skipped (missing data)");
            Console.WriteLine($"{AlreadyExists} skipped (already existed)");
            Console.WriteLine("-----------------------------\n");
        }
    }

    // Aggregated KDA per game per team, built from player rows.
    private record TeamKda(int Deaths, int Assists);

    /// <summary>
    /// Imports player stats, team stats, and draft entries for all games
    /// in a given tournament that have an ExternalId but no detail stats yet.
    /// </summary>
    public async Task ImportGameDetailsForTournamentAsync(string liquipediaSlug)
    {
        Console.WriteLine($"\nImporting game details for: {liquipediaSlug}");

        var games = await dbContext.Games
            .Where(game =>
                game.Match.Tournament.LiquipediaSlug == liquipediaSlug &&
                game.ExternalId != null)
            .Include(game => game.Match)
            .ToListAsync();

        if (games.Count == 0)
        {
            Console.WriteLine("  No games with ExternalId found — has backfill been run?");
            return;
        }

        var existingPlayerStatGameIds = await dbContext.GamePlayerStats
            .Where(playerStat => games.Select(game => game.Id).Contains(playerStat.GameId))
            .Select(playerStat => playerStat.GameId)
            .Distinct()
            .ToListAsync();

        var existingTeamStatGameIds = await dbContext.GameTeamStats
            .Where(teamStat => games.Select(game => game.Id).Contains(teamStat.GameId))
            .Select(teamStat => teamStat.GameId)
            .Distinct()
            .ToListAsync();

        var existingDraftGameIds = await dbContext.GameDraftEntries
            .Where(draftEntry => games.Select(game => game.Id).Contains(draftEntry.GameId))
            .Select(draftEntry => draftEntry.GameId)
            .Distinct()
            .ToListAsync();

        var gamesToProcess = games
            .Where(game =>
                !existingPlayerStatGameIds.Contains(game.Id) ||
                !existingTeamStatGameIds.Contains(game.Id) ||
                !existingDraftGameIds.Contains(game.Id))
            .ToList();

        Console.WriteLine($"  {games.Count} total games, {gamesToProcess.Count} need detail import");

        if (gamesToProcess.Count == 0)
        {
            Console.WriteLine("  All games already have detail stats.");
            return;
        }

        var gameByExternalId = gamesToProcess.ToDictionary(game => game.ExternalId!);

        var playerStats = new ImportStats();
        var teamStats = new ImportStats();
        var draftStats = new ImportStats();

        // Player stats first — returns deaths/assists per game per team for use in team stats.
        var kdaByGameAndTeam = await ImportPlayerStatsAsync(
            liquipediaSlug, gameByExternalId, existingPlayerStatGameIds, playerStats);

        await Task.Delay(Math.Max(0, _leaguepediaOptions.DelayBetweenGameDetailImportPhasesMilliseconds));

        await ImportTeamStatsAsync(
            liquipediaSlug, gameByExternalId, existingTeamStatGameIds, kdaByGameAndTeam, teamStats);

        await Task.Delay(Math.Max(0, _leaguepediaOptions.DelayBetweenGameDetailImportPhasesMilliseconds));

        await ImportDraftEntriesAsync(
            liquipediaSlug, gameByExternalId, existingDraftGameIds, draftStats);

        await dbContext.SaveChangesAsync();

        playerStats.Print("GamePlayerStats");
        teamStats.Print("GameTeamStats");
        draftStats.Print("GameDraftEntry");
    }

    /// <summary>
    /// Fetches all Cargo rows for a query using limit/offset pages and a stable <paramref name="orderBy"/>.
    /// </summary>
    private async Task<List<JsonElement>> FetchAllCargoPagesAsync(
        string tables,
        string fields,
        string where,
        string orderBy)
    {
        var pageSize = Math.Max(1, _leaguepediaOptions.CargoPageSize);
        var allRows = new List<JsonElement>();
        var offset = 0;
        var pageIndex = 0;

        while (true)
        {
            pageIndex++;
            var batch = await client.QueryAsync(
                tables: tables,
                fields: fields,
                where: where,
                orderBy: orderBy,
                limit: pageSize,
                offset: offset);

            Console.WriteLine($"  Cargo page {pageIndex}, +{batch.Count} rows (total {allRows.Count + batch.Count})");
            allRows.AddRange(batch);

            if (batch.Count < pageSize)
                break;

            offset += batch.Count;
        }

        return allRows;
    }

    // =====================================================================
    //  PLAYER STATS  (ScoreboardPlayers)
    // =====================================================================

    /// <summary>
    /// Returns a lookup of (gameId, teamNumber) → (totalDeaths, totalAssists)
    /// so team stats can include full KDA without a second API call.
    /// </summary>
    private async Task<Dictionary<(int GameId, int TeamNumber), TeamKda>> ImportPlayerStatsAsync(
        string liquipediaSlug,
        Dictionary<string, Game> gameByExternalId,
        List<int> alreadyImportedGameIds,
        ImportStats stats)
    {
        Console.WriteLine("  Fetching ScoreboardPlayers...");

        var rows = await FetchAllCargoPagesAsync(
            tables: "ScoreboardPlayers",
            fields: "GameId,Link,Side,IngameRole,Champion,Kills,Deaths,Assists,Gold,CS,DamageToChampions,VisionScore,Items,Trinket,SummonerSpells",
            where: $"OverviewPage=\"{liquipediaSlug}\"",
            orderBy: "GameId, Link");

        Console.WriteLine($"  Got {rows.Count} ScoreboardPlayers rows (all pages)");

        // (gameId, teamNumber) → accumulated deaths + assists
        var kdaAccumulator = new Dictionary<(int, int), (int Deaths, int Assists)>();

        foreach (var row in rows)
        {
            var leaguepediaGameId = row.GetProperty("GameId").GetString();
            if (string.IsNullOrWhiteSpace(leaguepediaGameId)) { stats.Skipped++; continue; }

            if (!gameByExternalId.TryGetValue(leaguepediaGameId, out var game)) { stats.Skipped++; continue; }
            if (alreadyImportedGameIds.Contains(game.Id)) { stats.AlreadyExists++; continue; }

            var playerName = row.GetProperty("Link").GetString();
            var ingameRole = row.GetProperty("IngameRole").GetString();
            var champion = row.GetProperty("Champion").GetString();

            if (string.IsNullOrWhiteSpace(playerName) ||
                string.IsNullOrWhiteSpace(ingameRole) ||
                string.IsNullOrWhiteSpace(champion))
            {
                stats.Skipped++;
                continue;
            }

            var sideStr = row.GetProperty("Side").GetString();
            if (!int.TryParse(sideStr, out var side) || side is not (1 or 2)) { stats.Skipped++; continue; }

            var teamNumber = ResolveTeamNumber(game, side);
            if (teamNumber == null) { stats.Skipped++; continue; }

            var kills = ParseInt(row, "Kills");
            var deaths = ParseInt(row, "Deaths");
            var assists = ParseInt(row, "Assists");
            var gold = ParseInt(row, "Gold");
            var cs = ParseInt(row, "CS");
            var damage = ParseInt(row, "DamageToChampions");
            var vision = ParseInt(row, "VisionScore");

            // Items: semicolon-separated since 2022
            var items = row.GetProperty("Items").GetString();
            var trinket = row.GetProperty("Trinket").GetString();

            // SummonerSpells: comma-separated e.g. "Flash,Teleport"
            var summonerSpells = row.GetProperty("SummonerSpells").GetString();
            string? spell1 = null, spell2 = null;
            if (!string.IsNullOrWhiteSpace(summonerSpells))
            {
                var parts = summonerSpells.Split(',', StringSplitOptions.TrimEntries);
                spell1 = parts.Length > 0 ? parts[0] : null;
                spell2 = parts.Length > 1 ? parts[1] : null;
            }

            dbContext.GamePlayerStats.Add(new GamePlayerStats(
                gameId: game.Id,
                teamNumber: teamNumber.Value,
                playerName: playerName,
                ingameRole: ingameRole,
                champion: champion,
                kills: kills,
                deaths: deaths,
                assists: assists,
                goldEarned: gold,
                creepScore: cs,
                damageDealtToChampions: damage,
                visionScore: vision,
                itemIds: string.IsNullOrWhiteSpace(items) ? null : items,
                trinketId: string.IsNullOrWhiteSpace(trinket) ? null : trinket,
                summonerSpell1Id: spell1,
                summonerSpell2Id: spell2
            ));

            stats.Imported++;

            // Accumulate deaths + assists per team for use in GameTeamStats
            var key = (game.Id, teamNumber.Value);
            if (!kdaAccumulator.TryGetValue(key, out var current))
                current = (0, 0);
            kdaAccumulator[key] = (current.Deaths + deaths, current.Assists + assists);
        }

        return kdaAccumulator.ToDictionary(
            deathAssistEntry => deathAssistEntry.Key,
            deathAssistEntry => new TeamKda(deathAssistEntry.Value.Deaths, deathAssistEntry.Value.Assists));
    }

    // =====================================================================
    //  TEAM STATS  (ScoreboardTeams)
    // =====================================================================

    private async Task ImportTeamStatsAsync(
        string liquipediaSlug,
        Dictionary<string, Game> gameByExternalId,
        List<int> alreadyImportedGameIds,
        Dictionary<(int GameId, int TeamNumber), TeamKda> kdaByGameAndTeam,
        ImportStats stats)
    {
        Console.WriteLine("  Fetching ScoreboardTeams...");

        var rows = await FetchAllCargoPagesAsync(
            tables: "ScoreboardTeams",
            fields: "GameId,Team,Side,Kills,Gold,Towers,Inhibitors,Barons,RiftHeralds,VoidGrubs,Dragons,Clouds,Infernals,Mountains,Oceans,Hextechs,Chemtechs,Elders,Gamelength_Number",
            where: $"OverviewPage=\"{liquipediaSlug}\"",
            orderBy: "GameId, Side");

        Console.WriteLine($"  Got {rows.Count} ScoreboardTeams rows (all pages)");

        foreach (var row in rows)
        {
            var leaguepediaGameId = row.GetProperty("GameId").GetString();
            if (string.IsNullOrWhiteSpace(leaguepediaGameId)) { stats.Skipped++; continue; }

            if (!gameByExternalId.TryGetValue(leaguepediaGameId, out var game)) { stats.Skipped++; continue; }
            if (alreadyImportedGameIds.Contains(game.Id)) { stats.AlreadyExists++; continue; }

            var sideStr = row.GetProperty("Side").GetString();
            var teamNumber = sideStr switch
            {
                "Blue" => ResolveTeamNumberFromSideString(game, "Blue"),
                "Red"  => ResolveTeamNumberFromSideString(game, "Red"),
                _      => null
            };
            if (teamNumber == null) { stats.Skipped++; continue; }

            var gameLengthMinutes = ParseFloat(row, "Gamelength_Number");
            var gameDurationSeconds = (int)(gameLengthMinutes * 60);

            // Pull deaths + assists accumulated from player rows
            kdaByGameAndTeam.TryGetValue((game.Id, teamNumber.Value), out var kda);
            var totalDeaths = kda?.Deaths ?? 0;
            var totalAssists = kda?.Assists ?? 0;

            dbContext.GameTeamStats.Add(new GameTeamStats(
                gameId: game.Id,
                teamNumber: teamNumber.Value,
                totalKills: ParseInt(row, "Kills"),
                totalDeaths: totalDeaths,
                totalAssists: totalAssists,
                totalGoldEarned: ParseInt(row, "Gold"),
                towersDestroyed: ParseInt(row, "Towers"),
                inhibitorsDestroyed: ParseInt(row, "Inhibitors"),
                baronsSlain: ParseInt(row, "Barons"),
                riftHeraldsSlain: ParseInt(row, "RiftHeralds"),
                voidGrubsSlain: ParseInt(row, "VoidGrubs"),
                totalDragonsSlain: ParseInt(row, "Dragons"),
                infernalDragonsSlain: ParseInt(row, "Infernals"),
                mountainDragonsSlain: ParseInt(row, "Mountains"),
                cloudDragonsSlain: ParseInt(row, "Clouds"),
                oceanDragonsSlain: ParseInt(row, "Oceans"),
                hextechDragonsSlain: ParseInt(row, "Hextechs"),
                chemtechDragonsSlain: ParseInt(row, "Chemtechs"),
                elderDragonsSlain: ParseInt(row, "Elders"),
                gameDurationSeconds: gameDurationSeconds
            ));

            stats.Imported++;
        }
    }

    // =====================================================================
    //  DRAFT  (PicksAndBansS7)
    // =====================================================================

    private async Task ImportDraftEntriesAsync(
        string liquipediaSlug,
        Dictionary<string, Game> gameByExternalId,
        List<int> alreadyImportedGameIds,
        ImportStats stats)
    {
        Console.WriteLine("  Fetching PicksAndBansS7...");

        // Cargo exposes pick columns as Team{n}Pick{k}; older docs used Team{n}Role{k}.
        var rows = await FetchAllCargoPagesAsync(
            tables: "PicksAndBansS7",
            fields: "GameId," +
                    "Team1Ban1,Team1Ban2,Team1Ban3,Team1Ban4,Team1Ban5," +
                    "Team2Ban1,Team2Ban2,Team2Ban3,Team2Ban4,Team2Ban5," +
                    "Team1Pick1,Team1Pick2,Team1Pick3,Team1Pick4,Team1Pick5," +
                    "Team2Pick1,Team2Pick2,Team2Pick3,Team2Pick4,Team2Pick5",
            where: $"OverviewPage=\"{liquipediaSlug}\"",
            orderBy: "GameId");

        Console.WriteLine($"  Got {rows.Count} PicksAndBansS7 rows (all pages)");

        foreach (var row in rows)
        {
            var leaguepediaGameId = row.GetProperty("GameId").GetString();
            if (string.IsNullOrWhiteSpace(leaguepediaGameId)) { stats.Skipped++; continue; }

            if (!gameByExternalId.TryGetValue(leaguepediaGameId, out var game)) { stats.Skipped++; continue; }
            if (alreadyImportedGameIds.Contains(game.Id)) { stats.AlreadyExists++; continue; }

            // Standard S7 draft order, SequenceNumber 1-20
            var draftSequence = new[]
            {
                (Team: 1, Phase: "Ban",  Slot: 1, Seq: 1),
                (Team: 2, Phase: "Ban",  Slot: 1, Seq: 2),
                (Team: 1, Phase: "Ban",  Slot: 2, Seq: 3),
                (Team: 2, Phase: "Ban",  Slot: 2, Seq: 4),
                (Team: 1, Phase: "Ban",  Slot: 3, Seq: 5),
                (Team: 2, Phase: "Ban",  Slot: 3, Seq: 6),
                (Team: 1, Phase: "Pick", Slot: 1, Seq: 7),
                (Team: 2, Phase: "Pick", Slot: 1, Seq: 8),
                (Team: 2, Phase: "Pick", Slot: 2, Seq: 9),
                (Team: 1, Phase: "Pick", Slot: 2, Seq: 10),
                (Team: 1, Phase: "Pick", Slot: 3, Seq: 11),
                (Team: 2, Phase: "Pick", Slot: 3, Seq: 12),
                (Team: 2, Phase: "Ban",  Slot: 4, Seq: 13),
                (Team: 1, Phase: "Ban",  Slot: 4, Seq: 14),
                (Team: 2, Phase: "Ban",  Slot: 5, Seq: 15),
                (Team: 1, Phase: "Ban",  Slot: 5, Seq: 16),
                (Team: 2, Phase: "Pick", Slot: 4, Seq: 17),
                (Team: 1, Phase: "Pick", Slot: 4, Seq: 18),
                (Team: 1, Phase: "Pick", Slot: 5, Seq: 19),
                (Team: 2, Phase: "Pick", Slot: 5, Seq: 20),
            };

            foreach (var (team, phase, slot, seq) in draftSequence)
            {
                var fieldName = phase == "Ban"
                    ? $"Team{team}Ban{slot}"
                    : $"Team{team}Pick{slot}";

                var champion = row.GetProperty(fieldName).GetString();
                if (string.IsNullOrWhiteSpace(champion) || champion == "None") continue;

                dbContext.GameDraftEntries.Add(new GameDraftEntry(
                    gameId: game.Id,
                    teamNumber: team,
                    phase: phase,
                    sequenceNumber: seq,
                    champion: champion
                ));

                stats.Imported++;
            }
        }
    }

    // =====================================================================
    //  HELPERS
    // =====================================================================

    private static int? ResolveTeamNumber(Game game, int leaguepediaSide)
    {
        var sideString = leaguepediaSide == 1 ? "Blue" : "Red";
        return ResolveTeamNumberFromSideString(game, sideString);
    }

    private static int? ResolveTeamNumberFromSideString(Game game, string side)
    {
        if (game.Team1Side == side) return 1;
        if (game.Team2Side == side) return 2;
        return null;
    }

    private static int? ResolveTeamNumberFromPickBan(Game game, int leaguepediaTeamIndex)
    {
        var side = leaguepediaTeamIndex == 1 ? "Blue" : "Red";
        return ResolveTeamNumberFromSideString(game, side);
    }

    private static int ParseInt(System.Text.Json.JsonElement row, string field)
    {
        var str = row.GetProperty(field).GetString();
        return int.TryParse(str, out var val) ? val : 0;
    }

    private static float ParseFloat(System.Text.Json.JsonElement row, string field)
    {
        var str = row.GetProperty(field).GetString();
        return float.TryParse(str, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var val) ? val : 0f;
    }
}
