using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using RiftVeil.Application.Dtos.Teams;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Import;

internal sealed record TeamCargoData(
    string? Short,
    string? LogoUrl,
    string? IconLogoUrl,
    string? Region,
    string? OverviewPage,
    bool MissingIconLogo);

/// <summary>
/// Imports tournaments, matches, games, and teams from Leaguepedia into the database.
/// </summary>
public class LeaguepediaImportService(
    LeaguepediaClient client,
    LeaguepediaTeamLogoVerifier logoVerifier,
    RiftVeilDbContext dbContext,
    IOptions<LeaguepediaClientOptions> leaguepediaOptions)
{
    private readonly LeaguepediaClientOptions _leaguepediaOptions = leaguepediaOptions.Value;
    private readonly Dictionary<string, TeamCargoData?> _teamCargoCache = new(StringComparer.Ordinal);

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
    public async Task ImportTournamentsAsync(string leagueName, int leagueId, string leagueShortName)
    {
        var stats = new ImportStats();
        var results = await client.QueryAsync(
            tables: "Tournaments",
            fields: "Name,DateStart,Date,League,Region,OverviewPage",
            where: $"League=\"{leagueName}\"",
            orderBy: "DateStart DESC",
            limit: 20
        );

        // Leaguepedia League labels are inconsistent for some regions (e.g. LPL, CBLOL, LCP).
        if (results.Count == 0)
        {
            var prefix = leagueShortName.Trim().ToUpperInvariant();
            Console.WriteLine(
                $"  No tournaments found for League=\"{leagueName}\". Falling back to OverviewPage LIKE \"{prefix}/%\".");
            results = await client.QueryAsync(
                tables: "Tournaments",
                fields: "Name,DateStart,Date,League,Region,OverviewPage",
                where: $"OverviewPage LIKE \"{prefix}/%\"",
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
        await PreloadTeamCargoAsync();

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
    /// Imports matches for tournaments active in the last <paramref name="recentDays"/> days
    /// (ongoing plus recently completed).
    /// </summary>
    public async Task ImportRecentMatchesAsync(int leagueId, int recentDays = ImportTournamentFilter.DefaultRecentDays)
    {
        await PreloadTeamCargoAsync();

        var matchStats = new ImportStats();
        var gameStats = new ImportStats();

        var now = DateTimeOffset.UtcNow;
        var tournaments = await ImportTournamentFilter
            .WhereRecent(
                dbContext.Tournaments.Where(tournament => tournament.LeagueId == leagueId),
                now,
                recentDays)
            .ToListAsync();

        Console.WriteLine($"  Found {tournaments.Count} tournament(s) in the last {recentDays} day(s)");

        foreach (var tournament in tournaments)
        {
            var importedCount = await ImportMatchesForTournamentAsync(tournament, matchStats, gameStats);

            if (importedCount > 0)
                await DelayBetweenTournamentsAsync();
        }

        matchStats.Print($"Matches (last {recentDays} days)");
        gameStats.Print($"Games (last {recentDays} days)");
    }

    /// <summary>
    /// Imports matches only for currently ongoing tournaments.
    /// </summary>
    public async Task ImportOngoingMatchesAsync(int leagueId)
    {
        await PreloadTeamCargoAsync();

        var matchStats = new ImportStats();
        var gameStats = new ImportStats();

        var now = DateTimeOffset.UtcNow;
        var tournaments = await ImportTournamentFilter
            .WhereOngoing(
                dbContext.Tournaments.Where(tournament => tournament.LeagueId == leagueId),
                now)
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

    /// <summary>
    /// Fills logo URLs, region, short name, and overview page from Leaguepedia Cargo <c>Teams</c>.
    /// </summary>
    public async Task<TeamBackfillResultDto> BackfillTeamMetadataAsync(int? leagueId = null, bool overwrite = false)
    {
        var teamsQuery = dbContext.Teams.AsQueryable();
        if (leagueId.HasValue)
        {
            teamsQuery = teamsQuery.Where(team =>
                dbContext.Matches.Any(match =>
                    match.Tournament.LeagueId == leagueId.Value
                    && (match.Team1Id == team.Id || match.Team2Id == team.Id)));
        }

        var teams = await teamsQuery.OrderBy(team => team.Name).ToListAsync();
        var updated = 0;
        var skipped = 0;
        var notFound = 0;
        var missingIconLogo = new List<TeamMissingIconDto>();

        Console.WriteLine($"  Backfilling team metadata for {teams.Count} team(s)...");

        foreach (var team in teams)
        {
            if (!overwrite
                && !string.IsNullOrWhiteSpace(team.Region)
                && !string.IsNullOrWhiteSpace(team.ExternalId)
                && !string.IsNullOrWhiteSpace(team.LogoUrl)
                && !string.IsNullOrWhiteSpace(team.IconLogoUrl))
            {
                skipped++;
                continue;
            }

            var (synced, missingIcon) = await SyncTeamMetadataInternalAsync(team, overwrite);
            if (synced)
            {
                updated++;
                if (missingIcon)
                    missingIconLogo.Add(new TeamMissingIconDto(team.Id, team.Name, team.ShortName));
            }
            else
            {
                notFound++;
            }
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine(
            $"  Team metadata: {updated} updated, {skipped} skipped, {notFound} not found, {missingIconLogo.Count} missing icon URL");

        if (missingIconLogo.Count > 0)
        {
            Console.WriteLine(
                $"  Missing icon URL (no square filename in Cargo Image): {string.Join(", ", missingIconLogo.Select(t => t.ShortName))}");
        }

        return new TeamBackfillResultDto(teams.Count, updated, skipped, notFound, missingIconLogo);
    }

    /// <summary>
    /// Loads Cargo <c>Teams</c> row for <paramref name="team"/> and applies metadata.
    /// Returns false when Leaguepedia has no matching team.
    /// </summary>
    public async Task<bool> SyncTeamMetadataFromLeaguepediaAsync(Team team, bool overwrite = false)
    {
        var (synced, _) = await SyncTeamMetadataInternalAsync(team, overwrite);
        return synced;
    }

    private async Task<(bool Synced, bool MissingIconLogo)> SyncTeamMetadataInternalAsync(
        Team team,
        bool overwrite)
    {
        var cargo = await ResolveTeamCargoAsync(team);
        if (cargo == null)
            return (false, false);

        await ApplyTeamCargoAsync(team, cargo, overwrite);
        return (true, cargo.MissingIconLogo);
    }

    private async Task<bool> IsShortNameAvailableAsync(int teamId, string shortName) =>
        !await IsShortNameTakenAsync(teamId, shortName);

    private async Task<bool> IsShortNameTakenAsync(int excludeTeamId, string shortName)
    {
        if (dbContext.Teams.Local.Any(t =>
                t.ShortName == shortName && (excludeTeamId == 0 || t.Id != excludeTeamId)))
        {
            return true;
        }

        return await dbContext.Teams.AnyAsync(t =>
            t.ShortName == shortName && t.Id != excludeTeamId);
    }

    private async Task<string> AllocateImportShortNameAsync(string teamName, string? cargoShort)
    {
        if (!string.IsNullOrWhiteSpace(cargoShort) && !await IsShortNameTakenAsync(0, cargoShort))
            return cargoShort;

        for (var n = 1; n < 10_000; n++)
        {
            var candidate = $"UNK{n}";
            if (!await IsShortNameTakenAsync(0, candidate))
                return candidate;
        }

        throw new InvalidOperationException($"Could not allocate a unique short name for '{teamName}'.");
    }

    private async Task PreloadTeamCargoAsync()
    {
        if (_teamCargoCache.Count > 0)
            return;

        var regions = new[] { "Europe", "EMEA", "CIS", "Turkey", "Korea", "North America", "China", "Americas", "Asia Pacific", "SEA" };

        foreach (var region in regions)
        {
            var results = await client.QueryAsync(
                tables: "Teams",
                fields: "Name,Short,Image,Region,OverviewPage",
                where: $"Region=\"{region}\"",
                limit: 500
            );

            foreach (var row in results)
                await CacheTeamCargoRowAsync(row, verifyIconUrl: false);
        }

        Console.WriteLine($"  Preloaded {_teamCargoCache.Count} team Cargo row(s) (icon URLs derived, no HTTP verify)");
    }

    private readonly Dictionary<string, Team> _teamCache = new();

    private async Task<Team> GetOrCreateTeamAsync(string teamName)
    {
        teamName = teamName.Trim();

        if (_teamCache.TryGetValue(teamName, out var cachedTeam))
            return cachedTeam;

        var team = await dbContext.Teams.FirstOrDefaultAsync(t => t.Name == teamName);

        if (team == null)
        {
            var cachedCargo = await ResolveTeamCargoForNameAsync(teamName);
            var shortName = await AllocateImportShortNameAsync(teamName, cachedCargo?.Short);
            if (string.IsNullOrWhiteSpace(cachedCargo?.Short))
            {
                Console.WriteLine(
                    $"  MANUAL_CHECK_REQUIRED: '{teamName}' — no Leaguepedia Teams row; assigned short '{shortName}'. Set name/short in Admin and run Sync LP");
            }

            team = new Team(
                teamName,
                shortName,
                region: cachedCargo?.Region,
                logoUrl: cachedCargo?.LogoUrl,
                iconLogoUrl: cachedCargo?.IconLogoUrl,
                externalId: cachedCargo?.OverviewPage);
            dbContext.Teams.Add(team);
            await dbContext.SaveChangesAsync();
        }
        else if ((await SyncTeamMetadataInternalAsync(team, overwrite: false)).Synced)
        {
            await dbContext.SaveChangesAsync();
        }

        _teamCache[teamName] = team;
        return team;
    }

    private async Task CacheTeamCargoRowAsync(JsonElement row, bool verifyIconUrl = false)
    {
        var cargo = await BuildTeamCargoDataAsync(row, verifyIconUrl);
        if (cargo == null)
            return;

        var name = row.GetProperty("Name").GetString()!;
        _teamCargoCache[name] = cargo;

        if (!string.IsNullOrWhiteSpace(cargo.OverviewPage)
            && !string.Equals(cargo.OverviewPage, name, StringComparison.Ordinal))
        {
            _teamCargoCache[cargo.OverviewPage] = cargo;
        }
    }

    private async Task<TeamCargoData?> BuildTeamCargoDataAsync(JsonElement row, bool verifyIconUrl = true)
    {
        var name = row.GetProperty("Name").GetString();
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var shortName = row.TryGetProperty("Short", out var shortProp) ? shortProp.GetString() : null;
        var normalizedShort = string.IsNullOrWhiteSpace(shortName) ? null : shortName.Trim().ToUpperInvariant();

        var image = row.TryGetProperty("Image", out var imageProp) ? imageProp.GetString() : null;
        var logoUrl = LeaguepediaImageUrls.TeamLogoFromCargoImage(image);
        var canDeriveSquare = LeaguepediaImageUrls.ToSquareLogoFileName(image) != null;
        var iconLogoUrl = verifyIconUrl
            ? await logoVerifier.ResolveVerifiedIconUrlAsync(image)
            : LeaguepediaImageUrls.TeamMarkFromCargoImage(image);
        // Missing only when Cargo has an image but no square filename can be derived (not HTTP failures).
        var missingIcon = !string.IsNullOrWhiteSpace(image) && !canDeriveSquare;

        var region = row.TryGetProperty("Region", out var regionProp) ? regionProp.GetString()?.Trim() : null;
        var overviewPage = row.TryGetProperty("OverviewPage", out var overviewProp) ? overviewProp.GetString()?.Trim() : null;

        return new TeamCargoData(normalizedShort, logoUrl, iconLogoUrl, region, overviewPage, missingIcon);
    }

    private async Task<bool> ApplyTeamCargoAsync(Team team, TeamCargoData cargo, bool overwrite)
    {
        var changed = false;

        if ((overwrite || string.IsNullOrWhiteSpace(team.LogoUrl)) && !string.IsNullOrWhiteSpace(cargo.LogoUrl))
        {
            team.SetLogoUrl(cargo.LogoUrl);
            changed = true;
        }

        var iconUrl = cargo.IconLogoUrl
            ?? LeaguepediaImageUrls.TeamMarkFromLogoUrl(team.LogoUrl)
            ?? LeaguepediaImageUrls.TeamMarkFromLogoUrl(cargo.LogoUrl);
        if ((overwrite || string.IsNullOrWhiteSpace(team.IconLogoUrl)) && !string.IsNullOrWhiteSpace(iconUrl))
        {
            team.SetIconLogoUrl(iconUrl);
            changed = true;
        }

        if ((overwrite || string.IsNullOrWhiteSpace(team.Region)) && !string.IsNullOrWhiteSpace(cargo.Region))
        {
            team.SetRegion(cargo.Region);
            changed = true;
        }

        if ((overwrite || string.IsNullOrWhiteSpace(team.ExternalId)) && !string.IsNullOrWhiteSpace(cargo.OverviewPage))
        {
            team.SetExternalId(cargo.OverviewPage);
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(cargo.Short)
            && (overwrite
                || string.IsNullOrWhiteSpace(team.ShortName)
                || team.ShortName.Equals("UNK", StringComparison.OrdinalIgnoreCase)
                || LooksLikeGuessedShort(team.ShortName, team.Name)))
        {
            var sameShort = team.ShortName.Equals(cargo.Short, StringComparison.OrdinalIgnoreCase);
            if (sameShort || await IsShortNameAvailableAsync(team.Id, cargo.Short))
            {
                if (!sameShort)
                    team.SetShortName(cargo.Short);
                changed = true;
            }
            else
            {
                Console.WriteLine(
                    $"  Skipped short '{cargo.Short}' for '{team.Name}' (already used by another team)");
            }
        }

        return changed;
    }

    private Task<TeamCargoData?> ResolveTeamCargoAsync(Team team) =>
        ResolveTeamCargoAsync(team.Name, team.ShortName, team.ExternalId);

    private Task<TeamCargoData?> ResolveTeamCargoForNameAsync(string teamName) =>
        ResolveTeamCargoAsync(teamName, shortName: null, externalId: null);

    private async Task<TeamCargoData?> ResolveTeamCargoAsync(
        string teamName,
        string? shortName,
        string? externalId)
    {
        if (_teamCargoCache.TryGetValue(teamName, out var cached))
            return cached;

        try
        {
            foreach (var where in BuildTeamCargoWhereClauses(teamName, shortName, externalId))
            {
                var results = await client.QueryAsync(
                    tables: "Teams",
                    fields: "Name,Short,Image,Region,OverviewPage",
                    where: where,
                    limit: 1);

                if (results.Count == 0)
                    continue;

                var cargo = await BuildTeamCargoDataAsync(results[0]);
                _teamCargoCache[teamName] = cargo;
                await CacheTeamCargoRowAsync(results[0]);
                return cargo;
            }

            _teamCargoCache[teamName] = null;
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Warning: Could not look up team Cargo for '{teamName}': {ex.Message}");
            _teamCargoCache[teamName] = null;
            return null;
        }
    }

    private static IEnumerable<string> BuildTeamCargoWhereClauses(
        string teamName,
        string? shortName,
        string? externalId)
    {
        yield return $"Name=\"{EscapeCargoValue(teamName)}\"";

        var stripped = StripDisambiguationSuffix(teamName);
        if (stripped != null && !stripped.Equals(teamName, StringComparison.Ordinal))
            yield return $"Name=\"{EscapeCargoValue(stripped)}\"";

        if (!string.IsNullOrWhiteSpace(externalId))
            yield return $"OverviewPage=\"{EscapeCargoValue(externalId)}\"";

        var overviewSlug = ToWikiOverviewSlug(teamName);
        if (!string.IsNullOrEmpty(overviewSlug))
            yield return $"OverviewPage=\"{EscapeCargoValue(overviewSlug)}\"";

        if (stripped != null)
        {
            var strippedSlug = ToWikiOverviewSlug(stripped);
            if (!string.IsNullOrEmpty(strippedSlug) && !strippedSlug.Equals(overviewSlug, StringComparison.Ordinal))
                yield return $"OverviewPage=\"{EscapeCargoValue(strippedSlug)}\"";
        }

        if (teamName.Length >= 4)
            yield return $"Name LIKE \"%{EscapeCargoValue(teamName)}%\"";

        if (stripped != null && stripped.Length >= 4)
            yield return $"Name LIKE \"%{EscapeCargoValue(stripped)}%\"";

        // Last resort: short may be correct even when it matches a 3-letter name prefix (e.g. DNF for DN Freecs).
        if (!string.IsNullOrWhiteSpace(shortName)
            && !shortName.Equals("UNK", StringComparison.OrdinalIgnoreCase)
            && shortName.Length is >= 2 and <= 6)
        {
            yield return $"Short=\"{EscapeCargoValue(shortName)}\"";
        }
    }

    private static string ToWikiOverviewSlug(string teamName) =>
        teamName.Trim().Replace(' ', '_');

    private static string EscapeCargoValue(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private static string? StripDisambiguationSuffix(string name)
    {
        var open = name.LastIndexOf(" (", StringComparison.Ordinal);
        if (open <= 0 || !name.EndsWith(')'))
            return null;

        return name[..open].Trim();
    }

    private static bool LooksLikeGuessedShort(string shortName, string teamName)
    {
        var compact = teamName.Replace(" ", "", StringComparison.Ordinal).ToUpperInvariant();
        if (compact.Length < 3)
            return false;

        var guess = compact[..3];
        return shortName.Equals(guess, StringComparison.OrdinalIgnoreCase);
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
