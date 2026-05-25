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
        public int SkippedMissingSides { get; set; }
        public int AlreadyExists { get; set; }

        public void Print(string type)
        {
            Console.WriteLine($"\n--- {type} Import Summary ---");
            Console.WriteLine($"{Imported} imported");
            Console.WriteLine($"{Skipped} skipped (missing data)");
            if (SkippedMissingSides > 0)
                Console.WriteLine($"{SkippedMissingSides} skipped (missing game sides)");
            Console.WriteLine($"{AlreadyExists} skipped (already existed)");
            Console.WriteLine("-----------------------------\n");
        }
    }

    // Aggregated KDA per game per team, built from player rows.
    private record TeamKda(int Deaths, int Assists);

    /// <summary>
    /// Imports player stats, team stats, and draft entries for all played games
    /// in a given tournament that have an ExternalId but no detail stats yet.
    /// Unplayed games (<see cref="Game.WinningTeam"/> == null) are excluded — Cargo has no
    /// rows for them and querying anyway burns rate-limit budget for nothing.
    /// </summary>
    public async Task ImportGameDetailsForTournamentAsync(string liquipediaSlug)
    {
        Console.WriteLine($"\nImporting game details for: {liquipediaSlug}");

        var games = await dbContext.Games
            .Where(game =>
                game.Match.Tournament.LiquipediaSlug == liquipediaSlug &&
                game.ExternalId != null &&
                game.WinningTeam != null)
            .Include(game => game.Match)
            .ToListAsync();

        if (games.Count == 0)
        {
            Console.WriteLine("  No played games with ExternalId found (nothing to import).");
            return;
        }

        await ImportGameDetailsForGamesAsync(liquipediaSlug, games);
    }

    /// <summary>
    /// Imports Leaguepedia stats/draft for a single game by local database id.
    /// </summary>
    /// <returns>null if the game does not exist.</returns>
    /// <exception cref="InvalidOperationException">Game has no ExternalId or tournament has no Liquipedia slug.</exception>
    public async Task<string?> ImportGameDetailsForGameIdAsync(int gameId)
    {
        var game = await dbContext.Games
            .Where(g => g.Id == gameId)
            .Include(g => g.Match)
            .ThenInclude(m => m.Tournament)
            .FirstOrDefaultAsync();

        if (game == null)
            return null;

        if (string.IsNullOrEmpty(game.ExternalId))
        {
            throw new InvalidOperationException(
                "Game has no Leaguepedia GameId. Re-run match import for this tournament, then try again.");
        }

        var slug = game.Match.Tournament.LiquipediaSlug;
        if (string.IsNullOrEmpty(slug))
            throw new InvalidOperationException("Tournament has no Liquipedia slug.");

        return await ImportGameDetailsForGamesAsync(slug, [game]);
    }

    /// <summary>
    /// Core import for a set of games that share the same Leaguepedia OverviewPage slug.
    /// </summary>
    private async Task<string> ImportGameDetailsForGamesAsync(string liquipediaSlug, List<Game> games)
    {
        // Sides are required to map Leaguepedia "Side=1/2" / "Blue"/"Red" → local Team1/Team2.
        // Without them ResolveTeamNumber returns null and every player/team row is silently skipped.
        await EnsureGameSidesAsync(liquipediaSlug, games);

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
            return "All games already have detail stats.";
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

        var skipDraftImport = gameByExternalId.Values.All(game => existingDraftGameIds.Contains(game.Id));
        if (skipDraftImport)
        {
            Console.WriteLine("  Skipping PicksAndBansS7 — draft entries already exist for all game(s) in this batch.");
        }
        else
        {
            await Task.Delay(Math.Max(0, _leaguepediaOptions.DelayBetweenGameDetailImportPhasesMilliseconds));

            await ImportDraftEntriesAsync(
                liquipediaSlug, gameByExternalId, existingDraftGameIds, draftStats);
        }

        await dbContext.SaveChangesAsync();

        playerStats.Print("GamePlayerStats");
        teamStats.Print("GameTeamStats");
        if (skipDraftImport)
        {
            Console.WriteLine("\n--- GameDraftEntry Import Summary ---");
            Console.WriteLine("(skipped — draft already present for all game(s) in this batch)");
            Console.WriteLine("-----------------------------\n");
        }
        else
        {
            draftStats.Print("GameDraftEntry");
        }

        return games.Count == 1
            ? $"Game detail import complete for game {games[0].Id}."
            : $"Game detail import complete for {gamesToProcess.Count} game(s) in tournament.";
    }

    /// <summary>
    /// Max <c>GameId</c> values per Cargo <c>where</c> clause — avoids huge URLs and server limits when importing many games.
    /// </summary>
    private const int MaxGameIdsPerCargoWhereClause = 40;

    /// <summary>
    /// Cargo can throw <c>internal_api_error_MWException</c> for wide <c>ScoreboardTeams</c> queries.
    /// We try the richest field list first, then drop columns until a query succeeds.
    /// <para>
    /// <c>Gamelength</c> (and the virtual <c>Gamelength_Number</c>) consistently triggers MWException
    /// against the current Cargo schema, so it is intentionally excluded — the ~15 s of failed retries
    /// is not worth the field. <see cref="ResolveTeamStatsGameDurationSeconds"/> falls back to the
    /// local <c>Game.Duration</c>, and a future Oracle's Elixir importer is the planned source for
    /// real in-game duration.
    /// </para>
    /// </summary>
    private static readonly string[] ScoreboardTeamsCargoFieldTiers =
    [
        "GameId,Team,Side,Kills,Gold,Towers,Inhibitors,Barons,RiftHeralds,VoidGrubs,Dragons,Clouds,Infernals,Mountains,Oceans,Hextechs,Chemtechs,Elders",
        "GameId,Team,Side,Kills,Gold,Towers,Inhibitors,Barons,RiftHeralds,Dragons,Clouds,Infernals,Mountains,Oceans,Hextechs,Chemtechs,Elders",
        "GameId,Team,Side,Kills,Gold,Towers,Inhibitors,Barons,RiftHeralds,Dragons",
        "GameId,Team,Side,Kills,Gold,Towers,Inhibitors",
        "GameId,Team,Side,Kills,Gold",
    ];

    /// <summary>
    /// Escape a value used inside Cargo double-quoted string literals.
    /// </summary>
    private static string EscapeCargoStringLiteral(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);

    /// <summary>
    /// Cargo filter: one overview page and only the requested Leaguepedia game ids (not the entire tournament).
    /// </summary>
    private static string BuildWhereOverviewAndGameIds(string overviewPage, IReadOnlyCollection<string> leaguepediaGameIds)
    {
        if (leaguepediaGameIds.Count == 0)
            throw new ArgumentException("At least one Leaguepedia GameId is required.", nameof(leaguepediaGameIds));

        var escPage = EscapeCargoStringLiteral(overviewPage);
        var gameClauses = leaguepediaGameIds
            .Select(id => $"GameId=\"{EscapeCargoStringLiteral(id)}\"");
        var orGroup = string.Join(" OR ", gameClauses);
        return $"OverviewPage=\"{escPage}\" AND ({orGroup})";
    }

    /// <summary>
    /// Fetches Cargo rows for <paramref name="overviewPage"/> restricted to the given Leaguepedia <c>GameId</c>s.
    /// Long id lists are split into multiple queries to keep URLs reasonable.
    /// </summary>
    private async Task<List<JsonElement>> FetchCargoForOverviewGameIdsAsync(
        string tables,
        string fields,
        string overviewPage,
        IReadOnlyCollection<string> leaguepediaGameIds,
        string orderBy)
    {
        var ids = leaguepediaGameIds as List<string> ?? leaguepediaGameIds.ToList();
        if (ids.Count == 0)
            return [];

        var merged = new List<JsonElement>();
        for (var i = 0; i < ids.Count; i += MaxGameIdsPerCargoWhereClause)
        {
            var chunkCount = Math.Min(MaxGameIdsPerCargoWhereClause, ids.Count - i);
            var chunk = ids.GetRange(i, chunkCount);
            var where = BuildWhereOverviewAndGameIds(overviewPage, chunk);
            var part = await FetchAllCargoPagesAsync(tables, fields, where, orderBy);
            merged.AddRange(part);
        }

        return merged;
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

    private async Task<(bool Succeeded, List<JsonElement> Rows)> FetchCargoForOverviewGameIdsWithOutcomeAsync(
        string tables,
        string fields,
        string overviewPage,
        IReadOnlyCollection<string> leaguepediaGameIds,
        string orderBy)
    {
        var ids = leaguepediaGameIds as List<string> ?? leaguepediaGameIds.ToList();
        if (ids.Count == 0)
            return (true, []);

        var merged = new List<JsonElement>();
        for (var i = 0; i < ids.Count; i += MaxGameIdsPerCargoWhereClause)
        {
            var chunkCount = Math.Min(MaxGameIdsPerCargoWhereClause, ids.Count - i);
            var chunk = ids.GetRange(i, chunkCount);
            var where = BuildWhereOverviewAndGameIds(overviewPage, chunk);
            var (ok, part) = await FetchAllCargoPagesWithOutcomeAsync(tables, fields, where, orderBy);
            if (!ok)
                return (false, []);

            merged.AddRange(part);
        }

        return (true, merged);
    }

    private async Task<(bool Succeeded, List<JsonElement> Rows)> FetchAllCargoPagesWithOutcomeAsync(
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
            var (ok, batch) = await client.QueryWithOutcomeAsync(
                tables: tables,
                fields: fields,
                where: where,
                orderBy: orderBy,
                limit: pageSize,
                offset: offset);

            if (!ok)
                return (false, []);

            Console.WriteLine($"  Cargo page {pageIndex}, +{batch.Count} rows (total {allRows.Count + batch.Count})");
            allRows.AddRange(batch);

            if (batch.Count < pageSize)
                break;

            offset += batch.Count;
        }

        return (true, allRows);
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
        Console.WriteLine($"  Fetching ScoreboardPlayers ({gameByExternalId.Count} GameId(s))...");

        var rows = await FetchCargoForOverviewGameIdsAsync(
            tables: "ScoreboardPlayers",
            fields: "GameId,Link,Side,IngameRole,Champion,Kills,Deaths,Assists,Gold,CS,DamageToChampions,VisionScore,Items,Trinket,SummonerSpells",
            overviewPage: liquipediaSlug,
            leaguepediaGameIds: gameByExternalId.Keys.ToList(),
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
            if (teamNumber == null) { stats.SkippedMissingSides++; continue; }

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
        Console.WriteLine($"  Fetching ScoreboardTeams ({gameByExternalId.Count} GameId(s))...");

        var gameIds = gameByExternalId.Keys.ToList();
        List<JsonElement> rows = [];
        foreach (var fields in ScoreboardTeamsCargoFieldTiers)
        {
            var (ok, fetched) = await FetchCargoForOverviewGameIdsWithOutcomeAsync(
                tables: "ScoreboardTeams",
                fields: fields,
                overviewPage: liquipediaSlug,
                leaguepediaGameIds: gameIds,
                orderBy: "GameId, Side");

            if (!ok)
            {
                Console.WriteLine(
                    "  ScoreboardTeams Cargo query failed after retries — trying a narrower field list...");
                continue;
            }

            rows = fetched;
            Console.WriteLine($"  Got {rows.Count} ScoreboardTeams rows (all pages) [Cargo fields: {fields}]");
            break;
        }

        if (rows.Count == 0 && gameIds.Count > 0)
            Console.WriteLine(
                "  Warning: No ScoreboardTeams rows returned for any field tier (Cargo failures or empty wiki data).");

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
            if (teamNumber == null) { stats.SkippedMissingSides++; continue; }

            var gameDurationSeconds = ResolveTeamStatsGameDurationSeconds(row, game);

            // Pull deaths + assists accumulated from player rows
            kdaByGameAndTeam.TryGetValue((game.Id, teamNumber.Value), out var kda);
            var totalDeaths = kda?.Deaths ?? 0;
            var totalAssists = kda?.Assists ?? 0;

            dbContext.GameTeamStats.Add(new GameTeamStats(
                gameId: game.Id,
                teamNumber: teamNumber.Value,
                totalKills: ParseIntOptional(row, "Kills"),
                totalDeaths: totalDeaths,
                totalAssists: totalAssists,
                totalGoldEarned: ParseIntOptional(row, "Gold"),
                towersDestroyed: ParseIntOptional(row, "Towers"),
                inhibitorsDestroyed: ParseIntOptional(row, "Inhibitors"),
                baronsSlain: ParseIntOptional(row, "Barons"),
                riftHeraldsSlain: ParseIntOptional(row, "RiftHeralds"),
                voidGrubsSlain: ParseIntOptional(row, "VoidGrubs"),
                totalDragonsSlain: ParseIntOptional(row, "Dragons"),
                infernalDragonsSlain: ParseIntOptional(row, "Infernals"),
                mountainDragonsSlain: ParseIntOptional(row, "Mountains"),
                cloudDragonsSlain: ParseIntOptional(row, "Clouds"),
                oceanDragonsSlain: ParseIntOptional(row, "Oceans"),
                hextechDragonsSlain: ParseIntOptional(row, "Hextechs"),
                chemtechDragonsSlain: ParseIntOptional(row, "Chemtechs"),
                elderDragonsSlain: ParseIntOptional(row, "Elders"),
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
        Console.WriteLine($"  Fetching PicksAndBansS7 ({gameByExternalId.Count} GameId(s))...");

        // Cargo exposes pick columns as Team{n}Pick{k}; older docs used Team{n}Role{k}.
        var rows = await FetchCargoForOverviewGameIdsAsync(
            tables: "PicksAndBansS7",
            fields: "GameId," +
                    "Team1Ban1,Team1Ban2,Team1Ban3,Team1Ban4,Team1Ban5," +
                    "Team2Ban1,Team2Ban2,Team2Ban3,Team2Ban4,Team2Ban5," +
                    "Team1Pick1,Team1Pick2,Team1Pick3,Team1Pick4,Team1Pick5," +
                    "Team2Pick1,Team2Pick2,Team2Pick3,Team2Pick4,Team2Pick5",
            overviewPage: liquipediaSlug,
            leaguepediaGameIds: gameByExternalId.Keys.ToList(),
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

                // PicksAndBansS7 Team1/Team2 = blue/red on the wiki, not necessarily local Team1/Team2.
                var localTeamNumber = ResolveTeamNumberFromPickBan(game, team);
                if (localTeamNumber == null)
                {
                    stats.SkippedMissingSides++;
                    continue;
                }

                dbContext.GameDraftEntries.Add(new GameDraftEntry(
                    gameId: game.Id,
                    teamNumber: localTeamNumber.Value,
                    phase: phase,
                    sequenceNumber: seq,
                    champion: champion
                ));

                stats.Imported++;
            }
        }
    }

    // =====================================================================
    //  SIDES BACKFILL  (MatchScheduleGame → ScoreboardTeams fallback)
    // =====================================================================

    /// <summary>
    /// Ensures every <see cref="Game"/> in <paramref name="games"/> has <c>Team1Side</c> and <c>Team2Side</c> set
    /// before player/team stats are imported. Without sides, every Cargo row would be silently skipped because
    /// <see cref="ResolveTeamNumber"/> cannot map "Blue"/"Red" → local Team1/Team2.
    ///
    /// Uses Cargo filtered by <c>GameId</c> (not the whole tournament) to reduce payload and rate-limit pressure,
    /// then falls back to <c>ScoreboardTeams</c> (Team + Side only) when schedule rows are missing.
    /// Persists with a single <see cref="DbContext.SaveChangesAsync"/> when any game was updated.
    /// </summary>
    private async Task EnsureGameSidesAsync(string overviewPage, List<Game> games)
    {
        var gamesNeedingSides = games
            .Where(game => game.Team1Side == null || game.Team2Side == null)
            .ToList();

        if (gamesNeedingSides.Count == 0)
            return;

        Console.WriteLine($"  Backfilling sides for {gamesNeedingSides.Count} game(s) (preflight)...");

        var matchIds = gamesNeedingSides.Select(game => game.MatchId).Distinct().ToList();
        var matchesWithTeams = await dbContext.Matches
            .Where(match => matchIds.Contains(match.Id))
            .Include(match => match.Team1)
            .Include(match => match.Team2)
            .ToDictionaryAsync(match => match.Id);

        var leaguepediaGameIds = gamesNeedingSides
            .Select(game => game.ExternalId!)
            .Distinct()
            .ToList();

        var rows = await FetchCargoForOverviewGameIdsAsync(
            tables: "MatchScheduleGame",
            fields: "MatchId,GameId,Blue,Red,N_GameInMatch=GameNumber",
            overviewPage: overviewPage,
            leaguepediaGameIds: leaguepediaGameIds,
            orderBy: "GameId");

        var updated = 0;

        if (rows.Count > 0)
        {
            var sidesByGameId = rows
                .Where(row => !string.IsNullOrWhiteSpace(row.GetProperty("GameId").GetString()))
                .GroupBy(row => row.GetProperty("GameId").GetString()!)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        var first = group.First();
                        return (
                            Blue: first.GetProperty("Blue").GetString(),
                            Red: first.GetProperty("Red").GetString()
                        );
                    });

            foreach (var game in gamesNeedingSides)
            {
                if (game.ExternalId == null) continue;
                if (game.Team1Side != null && game.Team2Side != null) continue;
                if (!sidesByGameId.TryGetValue(game.ExternalId, out var sides)) continue;
                if (string.IsNullOrWhiteSpace(sides.Blue) || string.IsNullOrWhiteSpace(sides.Red)) continue;
                if (!matchesWithTeams.TryGetValue(game.MatchId, out var match)) continue;

                if (!TrySetSidesFromBlueRedNames(game, match, sides.Blue, sides.Red))
                    Console.WriteLine(
                        $"  Warning: Could not map sides from schedule for {game.ExternalId} (Blue='{sides.Blue}', Red='{sides.Red}', local Team1='{match.Team1.Name}')");
                else
                    updated++;
            }
        }
        else
        {
            Console.WriteLine("  No MatchScheduleGame rows for sides backfill (filtered query) — will try ScoreboardTeams fallback.");
        }

        var stillNeedingSides = gamesNeedingSides
            .Where(game => game.Team1Side == null || game.Team2Side == null)
            .ToList();

        if (stillNeedingSides.Count > 0)
        {
            var idsForFallback = stillNeedingSides.Select(g => g.ExternalId!).Distinct().ToList();
            Console.WriteLine($"  ScoreboardTeams fallback (GameId,Team,Side) for {idsForFallback.Count} Leaguepedia game id(s)...");

            var teamRows = await FetchCargoForOverviewGameIdsAsync(
                tables: "ScoreboardTeams",
                fields: "GameId,Team,Side",
                overviewPage: overviewPage,
                leaguepediaGameIds: idsForFallback,
                orderBy: "GameId, Side");

            var rowsByGameId = teamRows
                .Where(row => !string.IsNullOrWhiteSpace(row.GetProperty("GameId").GetString()))
                .GroupBy(row => row.GetProperty("GameId").GetString()!)
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var game in stillNeedingSides)
            {
                if (game.ExternalId == null) continue;
                if (game.Team1Side != null && game.Team2Side != null) continue;
                if (!matchesWithTeams.TryGetValue(game.MatchId, out var match)) continue;
                if (!rowsByGameId.TryGetValue(game.ExternalId, out var gameRows) || gameRows.Count < 2)
                    continue;

                if (!TrySetSidesFromScoreboardTeamRows(game, match, gameRows))
                    Console.WriteLine(
                        $"  Warning: Could not map sides from ScoreboardTeams for {game.ExternalId} (local Team1='{match.Team1.Name}', Team2='{match.Team2.Name}')");
                else
                    updated++;
            }
        }

        if (updated > 0)
        {
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"  Backfilled sides for {updated} game(s).");
        }

        if (gamesNeedingSides.Any(game => game.Team1Side == null || game.Team2Side == null))
        {
            Console.WriteLine(
                "  Some games still lack Team1Side/Team2Side — player/team stats that need Blue/Red mapping may be skipped until sides are resolved.");
        }
    }

    /// <summary>
    /// Maps Leaguepedia Blue/Red team display names to local Team1/Team2 and calls <see cref="Game.SetSides"/>.
    /// </summary>
    private static bool TrySetSidesFromBlueRedNames(Game game, Match match, string blueTeamName, string redTeamName)
    {
        var isTeam1Blue = NameMatches(blueTeamName, match.Team1.Name)
                          || NameMatches(blueTeamName, match.Team1.ShortName);
        var isTeam1Red = NameMatches(redTeamName, match.Team1.Name)
                         || NameMatches(redTeamName, match.Team1.ShortName);

        if (!isTeam1Blue && !isTeam1Red)
            return false;

        var team1Side = isTeam1Blue ? "Blue" : "Red";
        var team2Side = isTeam1Blue ? "Red" : "Blue";
        game.SetSides(team1Side, team2Side);
        return true;
    }

    /// <summary>
    /// Uses <c>ScoreboardTeams</c> rows (Team display name + Side Blue/Red) for one <c>GameId</c>.
    /// </summary>
    private static bool TrySetSidesFromScoreboardTeamRows(Game game, Match match, List<JsonElement> gameRows)
    {
        string? blueTeam = null, redTeam = null;
        foreach (var row in gameRows)
        {
            var teamName = row.GetProperty("Team").GetString();
            var side = row.GetProperty("Side").GetString();
            if (string.IsNullOrWhiteSpace(teamName) || string.IsNullOrWhiteSpace(side))
                continue;

            if (side.Equals("Blue", StringComparison.OrdinalIgnoreCase))
                blueTeam = teamName;
            else if (side.Equals("Red", StringComparison.OrdinalIgnoreCase))
                redTeam = teamName;
        }

        if (string.IsNullOrWhiteSpace(blueTeam) || string.IsNullOrWhiteSpace(redTeam))
            return false;

        return TrySetSidesFromBlueRedNames(game, match, blueTeam, redTeam);
    }

    /// <summary>
    /// Compares a Leaguepedia team name (Cargo) to a local team name. Case-insensitive exact
    /// match, with a tolerance for MediaWiki disambiguation suffixes — e.g. Leaguepedia stores
    /// the LCS team as <c>"LYON (2024 American Team)"</c> to distinguish it from the older French
    /// LYON, while we keep the short, current-season name <c>"LYON"</c> in the DB. Without this
    /// tolerance, ScoreboardPlayers/ScoreboardTeams rows for those games would be silently
    /// skipped because side-mapping fails.
    /// <para>
    /// Wiki convention is <c>"Name (disambiguation text)"</c>, so a strict prefix check
    /// followed by " (" is safe — it can't accidentally match unrelated teams like "LYON Gaming"
    /// (no space-paren), only the disambiguation form.
    /// </para>
    /// </summary>
    private static bool NameMatches(string? cargoName, string? entityName)
    {
        if (string.IsNullOrWhiteSpace(cargoName) || string.IsNullOrWhiteSpace(entityName))
            return false;

        if (string.Equals(cargoName, entityName, StringComparison.OrdinalIgnoreCase))
            return true;

        return cargoName.StartsWith(entityName + " (", StringComparison.OrdinalIgnoreCase);
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

    /// <summary>
    /// Like <see cref="ParseInt"/> but returns 0 when <paramref name="field"/> is absent on the row,
    /// which happens for any column that wasn't requested by the winning <see cref="ScoreboardTeamsCargoFieldTiers"/> tier.
    /// Use this for every field that may live in only the richer tiers; <see cref="ParseInt"/> still
    /// throws on truly missing required fields and is appropriate for guaranteed columns.
    /// </summary>
    private static int ParseIntOptional(JsonElement row, string field)
    {
        if (!row.TryGetProperty(field, out var property))
            return 0;

        var str = property.GetString();
        return int.TryParse(str, out var val) ? val : 0;
    }

    /// <summary>
    /// Uses Cargo <c>Gamelength</c> when present; otherwise falls back to the local game's duration.
    /// </summary>
    private static int ResolveTeamStatsGameDurationSeconds(JsonElement row, Game game)
    {
        if (row.TryGetProperty("Gamelength", out var gl))
        {
            var parsed = ParseGameLengthSeconds(gl.GetString());
            if (parsed > 0)
                return parsed;
        }

        return game.Duration.HasValue ? (int)game.Duration.Value.TotalSeconds : 0;
    }

    private static float ParseFloat(System.Text.Json.JsonElement row, string field)
    {
        var str = row.GetProperty(field).GetString();
        return float.TryParse(str, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var val) ? val : 0f;
    }

    /// <summary>
    /// Parses Leaguepedia <c>Gamelength</c> values into seconds.
    /// Accepts <c>"mm:ss"</c> (e.g. <c>"32:14"</c>) and <c>"h:mm:ss"</c>; tolerates whitespace.
    /// Returns 0 when missing or unparseable so older/in-progress games don't fail the import.
    /// </summary>
    private static int ParseGameLengthSeconds(string? gameLength)
    {
        if (string.IsNullOrWhiteSpace(gameLength))
            return 0;

        var parts = gameLength.Trim().Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 2 or > 3)
            return 0;

        var invariant = System.Globalization.CultureInfo.InvariantCulture;
        if (parts.Length == 2 &&
            int.TryParse(parts[0], System.Globalization.NumberStyles.Integer, invariant, out var minutes) &&
            int.TryParse(parts[1], System.Globalization.NumberStyles.Integer, invariant, out var seconds))
        {
            return minutes * 60 + seconds;
        }

        if (parts.Length == 3 &&
            int.TryParse(parts[0], System.Globalization.NumberStyles.Integer, invariant, out var hours) &&
            int.TryParse(parts[1], System.Globalization.NumberStyles.Integer, invariant, out var hMinutes) &&
            int.TryParse(parts[2], System.Globalization.NumberStyles.Integer, invariant, out var hSeconds))
        {
            return hours * 3600 + hMinutes * 60 + hSeconds;
        }

        return 0;
    }
}
