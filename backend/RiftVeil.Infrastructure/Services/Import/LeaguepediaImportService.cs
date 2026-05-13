using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Imports tournaments, matches, games, and teams from Leaguepedia into the database.
/// </summary>
public class LeaguepediaImportService(
    LeaguepediaClient client,
    RiftVeilDbContext dbContext,
    IOptions<LeaguepediaClientOptions> leaguepediaOptions)
{
    private readonly LeaguepediaClientOptions _leaguepediaOptions = leaguepediaOptions.Value;
    private readonly Dictionary<string, string?> _shortNameCache = new();

    private class ImportStats
    {
        public int Read { get; set; }
        public int Imported { get; set; }
        public int Updated { get; set; }
        public int Existing { get; set; }
        public int Ignored { get; set; }

        public void Print(string type)
        {
            Console.WriteLine($"\n--- {type} Import Summary ---");
            Console.WriteLine($"{Read} entries read");
            Console.WriteLine($"{Imported} imported");
            if (Updated > 0)
                Console.WriteLine($"{Updated} updated");
            Console.WriteLine($"{Existing} skipped (already existed)");
            Console.WriteLine("-----------------------------\n");
        }
    }

    /// <summary>
    /// Imports tournaments for the given league from Leaguepedia.
    /// </summary>
    public async Task ImportTournamentsAsync(string leagueName, int leagueId)
    {
        var stats = new ImportStats();
        var results = await client.QueryAsync(
            tables: "Tournaments",
            fields: "Name,DateStart,Date,League,Region,OverviewPage",
            where: $"League=\"{leagueName}\"",
            orderBy: "DateStart DESC",
            limit: 20
        );

        // Leaguepedia occasionally uses inconsistent League labels for LPL rows.
        // If strict League filter returns no rows, fallback to OverviewPage prefix.
        if (results.Count == 0 && string.Equals(leagueName, "LPL", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("  No tournaments found for League=\"LPL\". Falling back to OverviewPage LIKE \"LPL/%\".");
            results = await client.QueryAsync(
                tables: "Tournaments",
                fields: "Name,DateStart,Date,League,Region,OverviewPage",
                where: "OverviewPage LIKE \"LPL/%\"",
                orderBy: "DateStart DESC",
                limit: 20
            );
        }

        stats.Read = results.Count;

        var existingBySlug = await dbContext.Tournaments
            .Where(tournament => tournament.LeagueId == leagueId)
            .Where(tournament => tournament.LiquipediaSlug != null)
            .ToDictionaryAsync(tournament => tournament.LiquipediaSlug!);

        foreach (var row in results)
        {
            var name = row.GetProperty("Name").GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                stats.Ignored++;
                continue;
            }

            var overviewPage = row.GetProperty("OverviewPage").GetString();
            if (string.IsNullOrWhiteSpace(overviewPage))
            {
                stats.Ignored++;
                continue;
            }

            var startDate = ParseDate(row.GetProperty("DateStart").GetString());
            var endDateStr = row.GetProperty("Date").GetString();
            DateTimeOffset? endDate = string.IsNullOrWhiteSpace(endDateStr)
                ? null
                : ParseDate(endDateStr);

            if (existingBySlug.TryGetValue(overviewPage, out var existingTournament))
            {
                var status = await DetermineStatusWithFallbackAsync(existingTournament.Id, startDate, endDate);
                existingTournament.SyncFromImport(
                    name: name,
                    startsAtUtc: startDate,
                    endsAtUtc: endDate,
                    status: status,
                    stage: ExtractStage(name)
                );
                stats.Updated++;
                continue;
            }

            var tournament = new Tournament(
                leagueId: leagueId,
                name: name,
                startsAtUtc: startDate,
                endsAtUtc: endDate,
                status: DetermineStatus(startDate, endDate),
                stage: ExtractStage(name),
                liquipediaSlug: overviewPage
            );

            dbContext.Tournaments.Add(tournament);
            existingBySlug[overviewPage] = tournament;
            stats.Imported++;
        }

        await dbContext.SaveChangesAsync();
        stats.Print("Tournaments");
    }

    /// <summary>
    /// Imports matches for all tournaments in the given league.
    /// </summary>
    public async Task ImportMatchesAsync(int leagueId)
    {
        var matchStats = new ImportStats();
        var gameStats = new ImportStats();

        var tournaments = await dbContext.Tournaments
            .Where(tournament => tournament.LeagueId == leagueId && tournament.LiquipediaSlug != null)
            .ToListAsync();

        foreach (var tournament in tournaments)
        {
            var importedCount = await ImportMatchesForTournamentAsync(tournament, matchStats, gameStats);

            if (importedCount > 0)
                await DelayBetweenTournamentsAsync();
        }

        matchStats.Print("Matches");
        gameStats.Print("Games");
    }

    /// <summary>
    /// Imports matches only for currently ongoing tournaments.
    /// </summary>
    public async Task ImportOngoingMatchesAsync(int leagueId)
    {
        var matchStats = new ImportStats();
        var gameStats = new ImportStats();

        var now = DateTimeOffset.UtcNow;
        var tournaments = await dbContext.Tournaments
            .Where(tournament => tournament.LeagueId == leagueId
                        && tournament.LiquipediaSlug != null
                        && tournament.StartsAtUtc <= now
                        && (tournament.EndsAtUtc == null || tournament.EndsAtUtc >= now))
            .ToListAsync();

        Console.WriteLine($"  Found {tournaments.Count} ongoing tournament(s)");

        foreach (var tournament in tournaments)
        {
            var importedCount = await ImportMatchesForTournamentAsync(tournament, matchStats, gameStats);

            if (importedCount > 0)
                await DelayBetweenTournamentsAsync();
        }

        matchStats.Print("Matches (ongoing)");
        gameStats.Print("Games (ongoing)");
    }

    private Task DelayBetweenTournamentsAsync()
    {
        var delay = Math.Max(0, _leaguepediaOptions.DelayBetweenMatchImportTournamentsMilliseconds);
        return delay > 0 ? Task.Delay(delay) : Task.CompletedTask;
    }

    private async Task<int> ImportMatchesForTournamentAsync(Tournament tournament, ImportStats matchStats, ImportStats gameStats)
    {
        Console.WriteLine($"Importing matches for: {tournament.Name}");

        var results = await client.QueryAsync(
            tables: "MatchSchedule",
            fields: "Team1,Team2,DateTime_UTC=DateTimeUTC,BestOf,Winner,Team1Score,Team2Score,OverviewPage,MatchId,Tab",
            where: $"OverviewPage=\"{tournament.LiquipediaSlug}\"",
            orderBy: "DateTime_UTC ASC",
            limit: 500
        );

        Console.WriteLine($"  Found {results.Count} matches");
        matchStats.Read += results.Count;

        // Load full match objects so we can update existing ones
        var existingMatches = await dbContext.Matches
            .Where(match => match.TournamentId == tournament.Id && match.ExternalId != null)
            .ToDictionaryAsync(match => match.ExternalId!);

        var startingImportedCount = matchStats.Imported;

        foreach (var row in results)
        {
            var matchId = row.GetProperty("MatchId").GetString();
            if (string.IsNullOrWhiteSpace(matchId))
            {
                matchStats.Ignored++;
                continue;
            }

            var winnerStr = row.GetProperty("Winner").GetString();
            var team1ScoreStr = row.GetProperty("Team1Score").GetString();
            var team2ScoreStr = row.GetProperty("Team2Score").GetString();
            var isFinished = !string.IsNullOrWhiteSpace(winnerStr) && winnerStr != "0";

            // Update existing match if it was Scheduled but now has a result
            if (existingMatches.TryGetValue(matchId, out var existingMatch))
            {
                if (existingMatch.Status == MatchStatus.Scheduled && isFinished
                    && int.TryParse(team1ScoreStr, out var t1ScoreUpd)
                    && int.TryParse(team2ScoreStr, out var t2ScoreUpd))
                {
                    existingMatch.MarkFinished(existingMatch.StartsAtUtc, existingMatch.StartsAtUtc.AddHours(2), t1ScoreUpd, t2ScoreUpd);
                    matchStats.Updated++;
                    Console.WriteLine($"  Updated match {matchId} to Finished ({t1ScoreUpd}-{t2ScoreUpd})");
                }
                else
                {
                    matchStats.Existing++;
                }
                continue;
            }

            // New match — create it
            var team1Name = row.GetProperty("Team1").GetString();
            var team2Name = row.GetProperty("Team2").GetString();
            if (string.IsNullOrWhiteSpace(team1Name) || string.IsNullOrWhiteSpace(team2Name))
            {
                matchStats.Ignored++;
                continue;
            }

            var team1 = await GetOrCreateTeamAsync(team1Name);
            var team2 = await GetOrCreateTeamAsync(team2Name);
            if (team1.Id == team2.Id)
            {
                matchStats.Ignored++;
                continue;
            }

            var startsAt = TryParseDate(row.GetProperty("DateTimeUTC").GetString());
            if (startsAt == null)
            {
                Console.WriteLine($"  Skipping match {matchId} — no valid date");
                matchStats.Ignored++;
                continue;
            }

            var bestOfStr = row.GetProperty("BestOf").GetString();
            var bestOf = int.TryParse(bestOfStr, out var bo) && bo is 1 or 2 or 3 or 5 ? bo : 1;

            var round = row.GetProperty("Tab").GetString();

            var match = new Match(
                tournamentId: tournament.Id,
                team1Id: team1.Id,
                team2Id: team2.Id,
                startsAtUtc: startsAt.Value,
                bestOf: bestOf,
                status: isFinished ? MatchStatus.Finished : MatchStatus.Scheduled,
                round: round,
                externalId: matchId
            );

            if (isFinished
                && int.TryParse(team1ScoreStr, out var t1Score)
                && int.TryParse(team2ScoreStr, out var t2Score))
            {
                match.MarkFinished(startsAt.Value, startsAt.Value.AddHours(2), t1Score, t2Score);
            }

            dbContext.Matches.Add(match);
            matchStats.Imported++;
        }

        await dbContext.SaveChangesAsync();

        var importedInThisBatch = matchStats.Imported - startingImportedCount;

        // No extra spacer between match and game imports — the LeaguepediaClient already
        // applies PostSuccessDelayMilliseconds after the match Cargo response.
        if (results.Count > 0)
            await ImportGamesForTournamentAsync(tournament, gameStats);

        return importedInThisBatch;
    }

    private async Task ImportGamesForTournamentAsync(Tournament tournament, ImportStats stats)
    {
        Console.WriteLine($"  Importing games for: {tournament.Name}");

        var results = await client.QueryAsync(
            tables: "MatchScheduleGame",
            fields: "MatchId,Blue,Red,Winner,Vod,N_GameInMatch=GameNumber",
            where: $"OverviewPage=\"{tournament.LiquipediaSlug}\"",
            orderBy: "N_GameInMatch ASC",
            limit: 500
        );

        stats.Read += results.Count;

        var tournamentMatches = await dbContext.Matches
            .Where(match => match.TournamentId == tournament.Id && match.ExternalId != null)
            .ToDictionaryAsync(match => match.ExternalId!);

        var matchIds = tournamentMatches.Values.Select(match => match.Id).ToList();

        // Load full game objects so we can update existing ones
        var existingGames = await dbContext.Games
            .Where(game => matchIds.Contains(game.MatchId))
            .ToListAsync();
        var existingGameKeys = existingGames
            .ToDictionary(game => $"{game.MatchId}:{game.GameNumber}");

        var teamIds = tournamentMatches.Values
            .SelectMany(match => new[] { match.Team1Id, match.Team2Id })
            .Distinct()
            .ToList();
        var teamsById = await dbContext.Teams
            .Where(team => teamIds.Contains(team.Id))
            .ToDictionaryAsync(team => team.Id);

        foreach (var row in results)
        {
            var matchId = row.GetProperty("MatchId").GetString();
            if (string.IsNullOrWhiteSpace(matchId))
            {
                stats.Ignored++;
                continue;
            }

            if (!tournamentMatches.TryGetValue(matchId, out var match))
            {
                stats.Ignored++;
                continue;
            }

            var gameNumberStr = row.GetProperty("GameNumber").GetString();
            if (!int.TryParse(gameNumberStr, out var gameNumber) || gameNumber <= 0)
            {
                stats.Ignored++;
                continue;
            }

            // Update existing game if WinningTeam is missing
            if (existingGameKeys.TryGetValue($"{match.Id}:{gameNumber}", out var existingGame))
            {
                var winnerStrGame = row.GetProperty("Winner").GetString();
                if (existingGame.WinningTeam == null && int.TryParse(winnerStrGame, out var wg) && wg is 1 or 2)
                {
                    var blueG = row.GetProperty("Blue").GetString();
                    if (!string.IsNullOrWhiteSpace(blueG) && teamsById.TryGetValue(match.Team1Id, out var t1g))
                    {
                        var isTeam1BlueG = string.Equals(blueG, t1g.Name, StringComparison.OrdinalIgnoreCase) ||
                                           string.Equals(blueG, t1g.ShortName, StringComparison.OrdinalIgnoreCase);
                        int winningTeamG = wg == 1 ? (isTeam1BlueG ? 1 : 2) : (isTeam1BlueG ? 2 : 1);
                        existingGame.SetWinningTeam(winningTeamG);
                        stats.Updated++;
                    }
                    else { stats.Existing++; }
                }
                else { stats.Existing++; }
                continue;
            }

            // New game — create it
            var blueTeam = row.GetProperty("Blue").GetString();
            var redTeam = row.GetProperty("Red").GetString();
            var winnerStr = row.GetProperty("Winner").GetString();

            string? team1Side = null;
            string? team2Side = null;
            int? winningTeam = null;

            if (!string.IsNullOrWhiteSpace(blueTeam) && !string.IsNullOrWhiteSpace(redTeam))
            {
                if (teamsById.TryGetValue(match.Team1Id, out var t1))
                {
                    var isTeam1Blue = string.Equals(blueTeam, t1.Name, StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(blueTeam, t1.ShortName, StringComparison.OrdinalIgnoreCase);
                    team1Side = isTeam1Blue ? "Blue" : "Red";
                    team2Side = isTeam1Blue ? "Red" : "Blue";

                    if (int.TryParse(winnerStr, out var w) && w is 1 or 2)
                    {
                        winningTeam = w == 1
                            ? (team1Side == "Blue" ? 1 : 2)
                            : (team1Side == "Red" ? 1 : 2);
                    }
                }
            }

            var vodUrl = row.GetProperty("Vod").GetString();

            var game = new Game(
                matchId: match.Id,
                gameNumber: gameNumber,
                team1Side: team1Side,
                team2Side: team2Side,
                winningTeam: winningTeam,
                vodUrl: string.IsNullOrWhiteSpace(vodUrl) ? null : vodUrl
            );

            dbContext.Games.Add(game);
            stats.Imported++;
        }

        await dbContext.SaveChangesAsync();
    }

    private async Task PreloadTeamShortNamesAsync()
    {
        var regions = new[] { "Europe", "EMEA", "CIS", "Turkey", "Korea", "North America", "China" };

        // No per-region spacer — LeaguepediaClient already applies PostSuccessDelayMilliseconds
        // after each successful response, and the process-wide semaphore guarantees serial execution.
        foreach (var region in regions)
        {
            var results = await client.QueryAsync(
                tables: "Teams",
                fields: "Name,Short",
                where: $"Region=\"{region}\"",
                limit: 100
            );

            foreach (var row in results)
            {
                var name = row.GetProperty("Name").GetString();
                var shortName = row.GetProperty("Short").GetString();
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(shortName))
                {
                    _shortNameCache[name] = shortName.Trim().ToUpperInvariant();
                }
            }
        }

        Console.WriteLine($"  Preloaded {_shortNameCache.Count} team short names");
    }

    private readonly Dictionary<string, Team> _teamCache = new();

    private async Task<Team> GetOrCreateTeamAsync(string teamName)
    {
        teamName = teamName.Trim();

        if (_teamCache.TryGetValue(teamName, out var cachedTeam))
            return cachedTeam;

        var team = await dbContext.Teams.FirstOrDefaultAsync(team => team.Name == teamName);

        if (team == null)
        {
            string shortName;
            if (_shortNameCache.TryGetValue(teamName, out var cached) && cached != null)
            {
                shortName = cached;
            }
            else
            {
                shortName = await LookupShortNameAsync(teamName)
                            ?? teamName.Replace(" ", "")[..Math.Min(teamName.Replace(" ", "").Length, 3)].ToUpperInvariant();

                if (shortName.Length <= 3)
                    Console.WriteLine($"  Warning: Using fallback short name '{shortName}' for '{teamName}' — fix manually");
            }

            var shortNameExists = await dbContext.Teams.AnyAsync(team => team.ShortName == shortName);
            if (shortNameExists)
                shortName = shortName[..Math.Min(shortName.Length, 17)] + dbContext.Teams.Local.Count;

            team = new Team(teamName, shortName);
            dbContext.Teams.Add(team);
            await dbContext.SaveChangesAsync();
        }

        _teamCache[teamName] = team;
        return team;
    }

    private async Task<string?> LookupShortNameAsync(string teamName)
    {
        if (_shortNameCache.TryGetValue(teamName, out var cached))
            return cached;

        // LeaguepediaClient.QueryAsync already paces requests; no extra delay needed here.
        try
        {
            var results = await client.QueryAsync(
                tables: "Teams",
                fields: "Name,Short",
                where: $"Name=\"{teamName}\"",
                limit: 1
            );

            if (results.Count > 0)
            {
                var shortName = results[0].GetProperty("Short").GetString();
                if (!string.IsNullOrWhiteSpace(shortName))
                {
                    var normalized = shortName.Trim().ToUpperInvariant();
                    _shortNameCache[teamName] = normalized;
                    return normalized;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Could not look up short name for '{teamName}': {ex.Message}");
        }

        _shortNameCache[teamName] = null;
        return null;
    }

    /// <summary>
    /// Extracts a stage label from a tournament name.
    /// E.g. "LEC 2026 Spring Season" -> "Spring 2026"
    ///      "LEC 2026 Spring Playoffs" -> "Spring Playoffs 2026"
    /// </summary>
    private static string? ExtractStage(string tournamentName)
    {
        var parts = tournamentName.Split(' ');
        var yearPart = parts.FirstOrDefault(part => part.Length == 4 && int.TryParse(part, out _));
        if (yearPart == null) return null;

        var yearIndex = Array.IndexOf(parts, yearPart);
        var afterYear = parts.Skip(yearIndex + 1)
            .Where(part => part != "Season")
            .ToArray();

        if (afterYear.Length == 0) return null;
        return $"{string.Join(" ", afterYear)} {yearPart}";
    }

    private static TournamentStatus DetermineStatus(DateTimeOffset start, DateTimeOffset? end)
    {
        var now = DateTimeOffset.UtcNow;
        if (end.HasValue && end.Value < now)
            return TournamentStatus.Finished;
        return start <= now ? TournamentStatus.Ongoing : TournamentStatus.Upcoming;
    }

    private async Task<TournamentStatus> DetermineStatusWithFallbackAsync(
        int tournamentId,
        DateTimeOffset start,
        DateTimeOffset? end)
    {
        var status = DetermineStatus(start, end);
        if (status != TournamentStatus.Ongoing)
            return status;

        var hasMatchData = await dbContext.Matches
            .AnyAsync(match => match.TournamentId == tournamentId);
        if (!hasMatchData)
            return status;

        var hasActiveOrPlannedMatches = await dbContext.Matches
            .AnyAsync(match =>
                match.TournamentId == tournamentId
                && (match.Status == MatchStatus.Live || match.Status == MatchStatus.Scheduled));

        return hasActiveOrPlannedMatches ? TournamentStatus.Ongoing : TournamentStatus.Finished;
    }

    private static DateTimeOffset? TryParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return null;

        var normalized = dateStr.Contains(' ')
            ? dateStr.Replace(' ', 'T') + "Z"
            : dateStr + "T00:00:00Z";

        if (DateTimeOffset.TryParse(normalized, out var result))
            return result;

        return null;
    }

    private static DateTimeOffset ParseDate(string? dateStr)
    {
        return TryParseDate(dateStr) ?? DateTimeOffset.UtcNow;
    }
    
    /// <summary>
    /// Backfills ExternalId (Leaguepedia GameId) on games that were imported before
    /// GameId was captured. Queries MatchScheduleGame for each match whose games
    /// are missing ExternalId and sets it from the API response.
    /// </summary>
    /// <returns>Count of games updated and tournaments skipped (no Cargo rows).</returns>
    public async Task<(int GamesUpdated, int TournamentsSkipped)> BackfillGameExternalIdsAsync(int leagueId)
    {
        var tournaments = await dbContext.Tournaments
            .Where(tournament => tournament.LeagueId == leagueId
                        && tournament.LiquipediaSlug != null
                        && tournament.Matches.Any(match => match.ExternalId != null && match.Games.Any(game => game.ExternalId == null)))
            .Include(tournament => tournament.Matches)
                .ThenInclude(match => match.Games)
            .ToListAsync();

        Console.WriteLine($"  Found {tournaments.Count} tournaments with games missing ExternalId");

        int updatedCount = 0;
        int skippedCount = 0;

        foreach (var tournament in tournaments)
        {
            Console.WriteLine($"  Fetching games for: {tournament.Name}");

            // One Cargo query per tournament (not per match) to limit API load.
            var results = await client.QueryAsync(
                tables: "MatchScheduleGame",
                fields: "MatchId,GameId,N_GameInMatch=GameNumber",
                where: $"OverviewPage=\"{tournament.LiquipediaSlug}\"",
                limit: 500
            );

            if (results.Count == 0)
            {
                Console.WriteLine($"  No results for {tournament.Name} — skipping");
                skippedCount++;
                continue;
            }
            
            var lpGamesByMatch = results
                .GroupBy(row => row.GetProperty("MatchId").GetString() ?? "")
                .Where(rowsByMatchId => rowsByMatchId.Key != "")
                .ToDictionary(
                    rowsByMatchId => rowsByMatchId.Key,
                    rowsByMatchId => rowsByMatchId.ToList()
                );

            foreach (var match in tournament.Matches.Where(scheduledMatch => scheduledMatch.ExternalId != null))
            {
                if (!lpGamesByMatch.TryGetValue(match.ExternalId!, out var lpGames))
                    continue;

                foreach (var row in lpGames)
                {
                    var leaguepediaGameId = row.GetProperty("GameId").GetString();
                    var gameNumberStr = row.GetProperty("GameNumber").GetString();

                    if (string.IsNullOrWhiteSpace(leaguepediaGameId))
                        continue;

                    if (!int.TryParse(gameNumberStr, out var gameNumber) || gameNumber <= 0)
                        continue;

                    var matchedGame = match.Games.FirstOrDefault(
                        game => game.GameNumber == gameNumber && game.ExternalId == null);
                    if (matchedGame == null)
                        continue;

                    matchedGame.SetExternalId(leaguepediaGameId);
                    updatedCount++;
                }
            }

            await dbContext.SaveChangesAsync();
            // No extra spacer — client paces requests via PostSuccessDelayMilliseconds.
        }

        Console.WriteLine($"\n--- Backfill Summary ---");
        Console.WriteLine($"{updatedCount} games updated with ExternalId");
        Console.WriteLine($"{skippedCount} tournaments skipped (no Leaguepedia data)");
        Console.WriteLine($"------------------------\n");

        return (updatedCount, skippedCount);
    }
    
    /// <summary>
    /// Backfills Team1Side and Team2Side for games that were imported before side data was available.
    /// </summary>
    public async Task<(int gamesUpdated, int tournamentsSkipped)> BackfillGameSidesAsync(int leagueId)
    {
        var tournaments = await dbContext.Tournaments
            .Where(tournament => tournament.LeagueId == leagueId
                        && tournament.LiquipediaSlug != null
                        && tournament.Matches.Any(match => match.Games.Any(game => game.ExternalId != null && game.Team1Side == null)))
            .Include(tournament => tournament.Matches)
                .ThenInclude(match => match.Games)
            .ToListAsync();

        Console.WriteLine($"  Found {tournaments.Count} tournaments with games missing sides");

        int updatedCount = 0;
        int skippedCount = 0;

        foreach (var tournament in tournaments)
        {
            Console.WriteLine($"  Fetching sides for: {tournament.Name}");

            var results = await client.QueryAsync(
                tables: "MatchScheduleGame",
                fields: "MatchId,GameId,Blue,Red,N_GameInMatch=GameNumber",
                where: $"OverviewPage=\"{tournament.LiquipediaSlug}\"",
                limit: 500
            );

            if (results.Count == 0)
            {
                Console.WriteLine($"  No results for {tournament.Name} — skipping");
                skippedCount++;
                continue;
            }

            // Build lookup: ExternalId → (Blue, Red)
            var sidesByGameId = results
                .Where(cargoRow => !string.IsNullOrWhiteSpace(cargoRow.GetProperty("GameId").GetString()))
                .ToDictionary(
                    cargoRow => cargoRow.GetProperty("GameId").GetString()!,
                    cargoRow => (
                        Blue: cargoRow.GetProperty("Blue").GetString(),
                        Red: cargoRow.GetProperty("Red").GetString()
                    )
                );

            foreach (var match in tournament.Matches)
            {
                foreach (var game in match.Games.Where(scheduledGame => scheduledGame.ExternalId != null && scheduledGame.Team1Side == null))
                {
                    if (!sidesByGameId.TryGetValue(game.ExternalId!, out var sides))
                        continue;

                    if (string.IsNullOrWhiteSpace(sides.Blue) || string.IsNullOrWhiteSpace(sides.Red))
                        continue;

                    // Determine which side Team1 is on — load teams if not already on this instance.
                    var matchWithTeams = await dbContext.Matches
                        .Include(loadedMatch => loadedMatch.Team1)
                        .Include(loadedMatch => loadedMatch.Team2)
                        .FirstOrDefaultAsync(loadedMatch => loadedMatch.Id == match.Id);

                    if (matchWithTeams == null) continue;

                    var isTeam1Blue =
                        string.Equals(sides.Blue, matchWithTeams.Team1.Name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sides.Blue, matchWithTeams.Team1.ShortName, StringComparison.OrdinalIgnoreCase);

                    var isTeam1Red =
                        string.Equals(sides.Red, matchWithTeams.Team1.Name, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sides.Red, matchWithTeams.Team1.ShortName, StringComparison.OrdinalIgnoreCase);

                    if (!isTeam1Blue && !isTeam1Red)
                    {
                        Console.WriteLine($"  Warning: Could not match team sides for game {game.ExternalId}");
                        continue;
                    }

                    var team1Side = isTeam1Blue ? "Blue" : "Red";
                    var team2Side = isTeam1Blue ? "Red" : "Blue";

                    game.SetSides(team1Side, team2Side);
                    updatedCount++;
                }
            }

            await dbContext.SaveChangesAsync();
            // No extra spacer — client paces requests via PostSuccessDelayMilliseconds.
        }

        Console.WriteLine($"\n--- Backfill Sides Summary ---");
        Console.WriteLine($"{updatedCount} games updated with sides");
        Console.WriteLine($"{skippedCount} tournaments skipped");
        Console.WriteLine($"------------------------------\n");

        return (updatedCount, skippedCount);
    }
}
