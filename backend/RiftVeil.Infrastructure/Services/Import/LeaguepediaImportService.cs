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

    // ──────────────────────────────────────────────
    // Tournaments
    // ──────────────────────────────────────────────

    /// <summary>
    /// Imports tournaments for the given league from Leaguepedia.
    /// </summary>
    public async Task ImportTournamentsAsync(string leagueName, int leagueId)
    {
        var results = await client.QueryAsync(
            tables: "Tournaments",
            fields: "Name,DateStart,Date,League,Region,OverviewPage",
            where: $"League=\"{leagueName}\"",
            orderBy: "DateStart DESC",
            limit: 20
        );

        foreach (var row in results)
        {
            var name = row.GetProperty("Name").GetString();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var overviewPage = row.GetProperty("OverviewPage").GetString();
            if (string.IsNullOrWhiteSpace(overviewPage))
                continue;

            var exists = await dbContext.Tournaments
                .AnyAsync(t => t.LiquipediaSlug == overviewPage);
            if (exists) continue;

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
        }

        await dbContext.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    // Matches
    // ──────────────────────────────────────────────

    /// <summary>
    /// Imports matches for all tournaments in the given league.
    /// </summary>
    public async Task ImportMatchesAsync(int leagueId)
    {
        await PreloadTeamShortNamesAsync();

        var tournaments = await dbContext.Tournaments
            .Where(t => t.LeagueId == leagueId && t.LiquipediaSlug != null)
            .ToListAsync();

        foreach (var tournament in tournaments)
        {
            await ImportMatchesForTournamentAsync(tournament);
            await Task.Delay(3000);
        }
    }

    private async Task ImportMatchesForTournamentAsync(Tournament tournament)
    {
        Console.WriteLine($"Importing matches for: {tournament.Name}");

        var results = await client.QueryAsync(
            tables: "MatchSchedule",
            fields: "Team1,Team2,DateTime_UTC=DateTimeUTC,BestOf,Winner,Team1Score,Team2Score,OverviewPage,MatchId",
            where: $"OverviewPage=\"{tournament.LiquipediaSlug}\"",
            orderBy: "DateTime_UTC ASC",
            limit: 100
        );

        Console.WriteLine($"  Found {results.Count} matches");

        foreach (var row in results)
        {
            var matchId = row.GetProperty("MatchId").GetString();
            if (string.IsNullOrWhiteSpace(matchId))
                continue;

            var exists = await dbContext.Matches
                .AnyAsync(m => m.ExternalId == matchId);
            if (exists) continue;

            var team1Name = row.GetProperty("Team1").GetString();
            var team2Name = row.GetProperty("Team2").GetString();
            if (string.IsNullOrWhiteSpace(team1Name) || string.IsNullOrWhiteSpace(team2Name))
                continue;

            var team1 = await GetOrCreateTeamAsync(team1Name);
            var team2 = await GetOrCreateTeamAsync(team2Name);
            if (team1.Id == team2.Id) continue;

            var startsAt = ParseDate(row.GetProperty("DateTimeUTC").GetString());

            var bestOfStr = row.GetProperty("BestOf").GetString();
            var bestOf = int.TryParse(bestOfStr, out var bo) && bo is 1 or 2 or 3 or 5 ? bo : 1;

            var winnerStr = row.GetProperty("Winner").GetString();
            var team1ScoreStr = row.GetProperty("Team1Score").GetString();
            var team2ScoreStr = row.GetProperty("Team2Score").GetString();

            var isFinished = !string.IsNullOrWhiteSpace(winnerStr) && winnerStr != "0";

            var match = new Match(
                tournamentId: tournament.Id,
                team1Id: team1.Id,
                team2Id: team2.Id,
                startsAtUtc: startsAt,
                bestOf: bestOf,
                status: isFinished ? MatchStatus.Finished : MatchStatus.Scheduled,
                externalId: matchId
            );

            if (isFinished
                && int.TryParse(team1ScoreStr, out var t1Score)
                && int.TryParse(team2ScoreStr, out var t2Score))
            {
                match.MarkFinished(startsAt, startsAt.AddHours(2), t1Score, t2Score);
            }

            dbContext.Matches.Add(match);
        }

        await dbContext.SaveChangesAsync();
        await ImportGamesForTournamentAsync(tournament);
    }

    // ──────────────────────────────────────────────
    // Games
    // ──────────────────────────────────────────────

    private async Task ImportGamesForTournamentAsync(Tournament tournament)
    {
        Console.WriteLine($"  Importing games for: {tournament.Name}");

        var results = await client.QueryAsync(
            tables: "MatchScheduleGame",
            fields: "MatchId,Blue,Red,Winner,Vod,N_GameInMatch=GameNumber",
            where: $"OverviewPage=\"{tournament.LiquipediaSlug}\"",
            orderBy: "N_GameInMatch ASC",
            limit: 500
        );

        var tournamentMatches = await dbContext.Matches
            .Where(m => m.TournamentId == tournament.Id && m.ExternalId != null)
            .ToDictionaryAsync(m => m.ExternalId!);

        foreach (var row in results)
        {
            var matchId = row.GetProperty("MatchId").GetString();
            if (string.IsNullOrWhiteSpace(matchId))
                continue;

            if (!tournamentMatches.TryGetValue(matchId, out var match))
                continue;

            var gameNumberStr = row.GetProperty("N_GameInMatch").GetString();
            if (!int.TryParse(gameNumberStr, out var gameNumber) || gameNumber <= 0)
                continue;

            var exists = await dbContext.Games
                .AnyAsync(g => g.MatchId == match.Id && g.GameNumber == gameNumber);
            if (exists) continue;

            var winnerStr = row.GetProperty("Winner").GetString();
            int? winningTeam = int.TryParse(winnerStr, out var w) && w is 1 or 2 ? w : null;

            var blueTeam = row.GetProperty("Blue").GetString();
            var redTeam = row.GetProperty("Red").GetString();

            string? team1Side = null;
            string? team2Side = null;
            if (!string.IsNullOrWhiteSpace(blueTeam) && !string.IsNullOrWhiteSpace(redTeam))
            {
                var t1 = await dbContext.Teams.FindAsync(match.Team1Id);
                if (t1 != null)
                {
                    var isTeam1Blue = string.Equals(blueTeam, t1.Name, StringComparison.OrdinalIgnoreCase) ||
                                      string.Equals(blueTeam, t1.ShortName, StringComparison.OrdinalIgnoreCase);
                    team1Side = isTeam1Blue ? "Blue" : "Red";
                    team2Side = isTeam1Blue ? "Red" : "Blue";
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
        }

        await dbContext.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    // Teams
    // ──────────────────────────────────────────────

    private async Task PreloadTeamShortNamesAsync()
    {
        var results = await client.QueryAsync(
            tables: "Teams",
            fields: "Name,Short",
            where: "Region=\"Europe\"",
            limit: 100
        );

        foreach (var row in results)
        {
            var name = row.GetProperty("Name").GetString();
            var shortName = row.GetProperty("Short").GetString();
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(shortName))
                _shortNameCache[name] = shortName.Trim().ToUpperInvariant();
        }

        Console.WriteLine($"  Preloaded {_shortNameCache.Count} team short names");
    }

    private async Task<Team> GetOrCreateTeamAsync(string teamName)
    {
        teamName = teamName.Trim();

        var team = await dbContext.Teams
            .FirstOrDefaultAsync(t => t.Name == teamName);
        if (team != null) return team;

        // Check preloaded cache, then try API lookup
        string shortName;
        if (_shortNameCache.TryGetValue(teamName, out var cached) && cached != null)
        {
            shortName = cached;
        }
        else
        {
            shortName = await LookupShortNameAsync(teamName)
                ?? teamName.Replace(" ", "")[..Math.Min(teamName.Replace(" ", "").Length, 5)].ToUpperInvariant();
        }

        // Handle collision
        var shortNameExists = await dbContext.Teams.AnyAsync(t => t.ShortName == shortName);
        if (shortNameExists)
            shortName = shortName[..Math.Min(shortName.Length, 17)] + dbContext.Teams.Local.Count;

        team = new Team(teamName, shortName);
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

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

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static TournamentStatus DetermineStatus(DateTimeOffset start, DateTimeOffset? end)
    {
        var now = DateTimeOffset.UtcNow;
        if (end.HasValue && end.Value < now) return TournamentStatus.Finished;
        return start <= now ? TournamentStatus.Ongoing : TournamentStatus.Upcoming;
    }

    private static DateTimeOffset ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr))
            return DateTimeOffset.UtcNow;

        var normalized = dateStr.Contains(' ')
            ? dateStr.Replace(' ', 'T') + "Z"
            : dateStr + "T00:00:00Z";

        return DateTimeOffset.Parse(normalized);
    }
}