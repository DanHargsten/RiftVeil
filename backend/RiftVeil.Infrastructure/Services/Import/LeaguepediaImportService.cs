using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Imports tournaments, matches, games, and teams from Leaguepedia into the database.
/// </summary>
public class LeaguepediaImportService(LeaguepediaClient client, RiftVeilDbContext dbContext)
{
    private readonly Dictionary<string, string?> _shortNameCache = new();

    private class ImportStats
    {
        public int Read { get; set; }
        public int Imported { get; set; }
        public int AlreadyExisted { get; set; }
        public int Ignored { get; set; }
        public int Errors { get; set; }

        public void Print(string type)
        {
            Console.WriteLine($"\n--- {type} Import Summary ---");
            Console.WriteLine($"Done");
            Console.WriteLine($"{Read} entries read");
            Console.WriteLine($"{Imported} imported");
            Console.WriteLine($"{AlreadyExisted + Ignored + Errors} ignored");
            Console.WriteLine($"   {AlreadyExisted}: already existed");
            if (Ignored > 0) Console.WriteLine($"   {Ignored}: missing required data");
            if (Errors > 0) Console.WriteLine($"   {Errors}: errors");
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

        stats.Read = results.Count;

        var existingSlugs = (await dbContext.Tournaments
            .Where(t => t.LeagueId == leagueId)
            .Select(t => t.LiquipediaSlug)
            .Where(s => s != null)
            .Select(s => s!)
            .ToListAsync()).ToHashSet();

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

            if (existingSlugs.Contains(overviewPage))
            {
                stats.AlreadyExisted++;
                continue;
            }

            var startDate = ParseDate(row.GetProperty("DateStart").GetString());
            var endDateStr = row.GetProperty("Date").GetString();
            DateTimeOffset? endDate = string.IsNullOrWhiteSpace(endDateStr)
                ? null
                : ParseDate(endDateStr);

            var tournament = new Tournament(
                leagueId: leagueId,
                name: name,
                startsAtUtc: startDate,
                endsAtUtc: endDate,
                status: DetermineStatus(startDate, endDate),
                liquipediaSlug: overviewPage
            );

            dbContext.Tournaments.Add(tournament);
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

        await PreloadTeamShortNamesAsync();

        var tournaments = await dbContext.Tournaments
            .Where(t => t.LeagueId == leagueId && t.LiquipediaSlug != null)
            .ToListAsync();

        foreach (var tournament in tournaments)
        {
            // Skip tournaments that already have matches imported
            var existingMatchCount = await dbContext.Matches
                .CountAsync(m => m.TournamentId == tournament.Id);
            
            if (existingMatchCount > 0)
            {
                Console.WriteLine($"Skipping {tournament.Name} — already has {existingMatchCount} matches");
                continue;
            }
            
            var importedCount = await ImportMatchesForTournamentAsync(tournament, matchStats, gameStats);
            
            if (importedCount > 0)
            {
                // Wait between tournaments to avoid rate limiting
                Console.WriteLine($"  Waiting 10s before next tournament...");
                await Task.Delay(10_000);
            }
            else
            {
                Console.WriteLine($"  No new matches imported for {tournament.Name}, skipping delay.");
            }
        }

        matchStats.Print("Matches");
        gameStats.Print("Games");
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

        var existingMatchExternalIds = (await dbContext.Matches
            .Where(m => m.TournamentId == tournament.Id)
            .Select(m => m.ExternalId)
            .Where(id => id != null)
            .Select(id => id!)
            .ToListAsync()).ToHashSet();

        var startingImportedCount = matchStats.Imported;

        foreach (var row in results)
        {
            var matchId = row.GetProperty("MatchId").GetString();
            if (string.IsNullOrWhiteSpace(matchId))
            {
                matchStats.Ignored++;
                continue;
            }

            if (existingMatchExternalIds.Contains(matchId))
            {
                matchStats.AlreadyExisted++;
                continue;
            }

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

            var startsAt = ParseDate(row.GetProperty("DateTimeUTC").GetString());

            var bestOfStr = row.GetProperty("BestOf").GetString();
            var bestOf = int.TryParse(bestOfStr, out var bo) && bo is 1 or 2 or 3 or 5 ? bo : 1;

            var winnerStr = row.GetProperty("Winner").GetString();
            var team1ScoreStr = row.GetProperty("Team1Score").GetString();
            var team2ScoreStr = row.GetProperty("Team2Score").GetString();
            
            // Round from Leaguepedia Tab field (e.g. "Quarterfinals", "Semifinals", "Grand Final")
            var round = row.GetProperty("Tab").GetString();

            var isFinished = !string.IsNullOrWhiteSpace(winnerStr) && winnerStr != "0";

            var match = new Match(
                tournamentId: tournament.Id,
                team1Id: team1.Id,
                team2Id: team2.Id,
                startsAtUtc: startsAt,
                bestOf: bestOf,
                status: isFinished ? MatchStatus.Finished : MatchStatus.Scheduled,
                round: round,
                externalId: matchId
            );

            if (isFinished
                && int.TryParse(team1ScoreStr, out var t1Score)
                && int.TryParse(team2ScoreStr, out var t2Score))
            {
                match.MarkFinished(startsAt, startsAt.AddHours(2), t1Score, t2Score);
            }

            dbContext.Matches.Add(match);
            matchStats.Imported++;
        }

        await dbContext.SaveChangesAsync();
        
        var importedInThisBatch = matchStats.Imported - startingImportedCount;

        // Wait before fetching games if we actually imported something or if we have results to process
        if (results.Count > 0)
        {
            if (importedInThisBatch > 0)
            {
                await Task.Delay(5000);
            }
            await ImportGamesForTournamentAsync(tournament, gameStats);
        }

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
            .Where(m => m.TournamentId == tournament.Id && m.ExternalId != null)
            .ToDictionaryAsync(m => m.ExternalId!);

        // Batch: load all existing game keys for this tournament using string keys
        var matchIds = tournamentMatches.Values.Select(m => m.Id).ToList();
        var existingGameKeys = (await dbContext.Games
            .Where(g => matchIds.Contains(g.MatchId))
            .Select(g => new { g.MatchId, g.GameNumber })
            .ToListAsync())
            .Select(g => $"{g.MatchId}:{g.GameNumber}")
            .ToHashSet();

        // Batch: pre-load teams for side mapping (instead of FindAsync per game)
        var teamIds = tournamentMatches.Values
            .SelectMany(m => new[] { m.Team1Id, m.Team2Id })
            .Distinct()
            .ToList();
        var teamsById = await dbContext.Teams
            .Where(t => teamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id);

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

            if (existingGameKeys.Contains($"{match.Id}:{gameNumber}"))
            {
                stats.AlreadyExisted++;
                continue;
            }
            
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

                    // Winner from Leaguepedia: 1=Blue won, 2=Red won
                    // Map to our model: 1=Team1 won, 2=Team2 won
                    if (int.TryParse(winnerStr, out var w) && w is 1 or 2)
                    {
                        if (w == 1) // Blue won
                        {
                            winningTeam = team1Side == "Blue" ? 1 : 2;
                        }
                        else // Red won
                        {
                            winningTeam = team1Side == "Red" ? 1 : 2;
                        }
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
        // Load teams from multiple regions to cover EMEA rosters
        var regions = new[] { "Europe", "EMEA", "CIS", "Turkey" };

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
            
            if (results.Count > 0)
            {
                await Task.Delay(5000); // Breathe between region queries
            }
        }

        Console.WriteLine($"  Preloaded {_shortNameCache.Count} team short names");
    }

    private readonly Dictionary<string, Team> _teamCache = new();

    private async Task<Team> GetOrCreateTeamAsync(string teamName)
    {
        teamName = teamName.Trim();

        if (_teamCache.TryGetValue(teamName, out var cachedTeam))
        {
            return cachedTeam;
        }

        var team = await dbContext.Teams
            .FirstOrDefaultAsync(t => t.Name == teamName);
        
        if (team == null)
        {
            // Check preloaded cache, then try API lookup, then fall back to first 3 chars
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
                {
                    Console.WriteLine($"  Warning: Using fallback short name '{shortName}' for '{teamName}' — fix manually");
                }
            }

            // Handle collision
            var shortNameExists = await dbContext.Teams.AnyAsync(t => t.ShortName == shortName);
            if (shortNameExists)
            {
                shortName = shortName[..Math.Min(shortName.Length, 17)] + dbContext.Teams.Local.Count;
            }

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

        await Task.Delay(2000);

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

    private static TournamentStatus DetermineStatus(DateTimeOffset start, DateTimeOffset? end)
    {
        var now = DateTimeOffset.UtcNow;
        if (end.HasValue && end.Value < now)
        {
            return TournamentStatus.Finished;
        }
        return start <= now ? TournamentStatus.Ongoing : TournamentStatus.Upcoming;
    }

    private static DateTimeOffset ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
        {
            return DateTimeOffset.UtcNow;
        }

        var normalized = dateStr.Contains(' ')
            ? dateStr.Replace(' ', 'T') + "Z"
            : dateStr + "T00:00:00Z";

        return DateTimeOffset.Parse(normalized);
    }
}