using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;
using RiftVeil.Infrastructure.Services.Import;

namespace RiftVeil.Api.Tests.Services;

public class LeaguepediaImportServiceTests
{
    [Fact]
    public async Task ImportMatchesAsync_ExistingScheduledMatchWithTbdOpponent_ReplacesOpponentWhenResolved()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedPlayoffScenarioAsync(dbContext, existingMatchFinished: false);

        var importService = CreateImportService(
            dbContext,
            CreateCargoJson([
                CreateMatchScheduleRow(
                    team1: "LOUD",
                    team2: "LOS",
                    winner: "0",
                    team1Score: "",
                    team2Score: "",
                    dateTimeUtc: "2026-04-11 18:00:00")
            ]));

        await importService.ImportMatchesAsync(seeded.LeagueId);

        var updatedMatch = await dbContext.Matches
            .Include(match => match.Team1)
            .Include(match => match.Team2)
            .SingleAsync(match => match.ExternalId == seeded.MatchExternalId);

        Assert.Equal("LOUD", updatedMatch.Team1.ShortName);
        Assert.Equal("LOS", updatedMatch.Team2.ShortName);
        Assert.Equal(MatchStatus.Scheduled, updatedMatch.Status);
        Assert.Null(updatedMatch.Team1Score);
        Assert.Null(updatedMatch.Team2Score);
    }

    [Fact]
    public async Task ImportMatchesAsync_ExistingScheduledMatch_UpdatesOpponentAndMarksFinished()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedPlayoffScenarioAsync(dbContext, existingMatchFinished: false);

        var importService = CreateImportService(
            dbContext,
            CreateCargoJson([
                CreateMatchScheduleRow(
                    team1: "LOUD",
                    team2: "LOS",
                    winner: "1",
                    team1Score: "3",
                    team2Score: "2",
                    dateTimeUtc: "2026-04-11 18:00:00")
            ]));

        await importService.ImportMatchesAsync(seeded.LeagueId);

        var updatedMatch = await dbContext.Matches
            .Include(match => match.Team2)
            .SingleAsync(match => match.ExternalId == seeded.MatchExternalId);

        Assert.Equal("LOS", updatedMatch.Team2.ShortName);
        Assert.Equal(MatchStatus.Finished, updatedMatch.Status);
        Assert.Equal(3, updatedMatch.Team1Score);
        Assert.Equal(2, updatedMatch.Team2Score);
    }

    [Fact]
    public async Task ImportMatchesAsync_ExistingFinishedMatch_StillSyncsParticipants()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedPlayoffScenarioAsync(dbContext, existingMatchFinished: true);

        var importService = CreateImportService(
            dbContext,
            CreateCargoJson([
                CreateMatchScheduleRow(
                    team1: "LOUD",
                    team2: "LOS",
                    winner: "1",
                    team1Score: "3",
                    team2Score: "1",
                    dateTimeUtc: "2026-04-11 18:00:00")
            ]));

        await importService.ImportMatchesAsync(seeded.LeagueId);

        var updatedMatch = await dbContext.Matches
            .Include(match => match.Team2)
            .SingleAsync(match => match.ExternalId == seeded.MatchExternalId);

        Assert.Equal("LOS", updatedMatch.Team2.ShortName);
        Assert.Equal(MatchStatus.Finished, updatedMatch.Status);
    }

    private static LeaguepediaImportService CreateImportService(RiftVeilDbContext dbContext, string matchScheduleJson)
    {
        var options = Options.Create(new LeaguepediaClientOptions
        {
            PostSuccessDelayMilliseconds = 0,
            DelayBetweenMatchImportTournamentsMilliseconds = 0,
            RateLimitMaxAttempts = 1,
            MaxTransientRetriesPerQuery = 1,
        });

        var httpClient = new HttpClient(new StubLeaguepediaHandler(matchScheduleJson));
        var leaguepediaClient = new LeaguepediaClient(httpClient, options);
        var logoVerifier = new LeaguepediaTeamLogoVerifier(leaguepediaClient);
        return new LeaguepediaImportService(leaguepediaClient, logoVerifier, dbContext, options);
    }

    private static async Task<(int LeagueId, string MatchExternalId)> SeedPlayoffScenarioAsync(
        RiftVeilDbContext dbContext,
        bool existingMatchFinished)
    {
        var league = new League("Campeonato Brasileiro de League of Legends", "CBLOL", "Brazil");
        dbContext.Leagues.Add(league);
        await dbContext.SaveChangesAsync();

        var tournament = new Tournament(
            leagueId: league.Id,
            name: "CBLOL Split 1 Playoffs 2026",
            startsAtUtc: DateTimeOffset.Parse("2026-04-01T00:00:00Z"),
            endsAtUtc: DateTimeOffset.Parse("2026-04-30T00:00:00Z"),
            status: TournamentStatus.Ongoing,
            liquipediaSlug: "CBLOL/2026_Season/Split_1_Playoffs");
        dbContext.Tournaments.Add(tournament);

        var loud = new Team("LOUD", "LOUD", "Brazil");
        var tbd = new Team("To Be Decided", "TBD", "Brazil");
        var los = new Team("Los Grandes", "LOS", "Brazil");
        dbContext.Teams.AddRange(loud, tbd, los);
        await dbContext.SaveChangesAsync();

        var match = new Match(
            tournamentId: tournament.Id,
            team1Id: loud.Id,
            team2Id: tbd.Id,
            startsAtUtc: DateTimeOffset.Parse("2026-04-11T18:00:00Z"),
            bestOf: 5,
            status: existingMatchFinished ? MatchStatus.Finished : MatchStatus.Scheduled,
            round: "Round 2",
            externalId: "CBLOL2026-MATCH-42");

        if (existingMatchFinished)
            match.MarkFinished(match.StartsAtUtc, match.StartsAtUtc.AddHours(3), 3, 0);

        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync();

        return (league.Id, "CBLOL2026-MATCH-42");
    }

    private static RiftVeilDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<RiftVeilDbContext>()
            .UseInMemoryDatabase($"LeaguepediaImportServiceTests_{Guid.NewGuid()}")
            .Options;

        return new RiftVeilDbContext(options);
    }

    private static Dictionary<string, string?> CreateMatchScheduleRow(
        string team1,
        string team2,
        string winner,
        string team1Score,
        string team2Score,
        string dateTimeUtc) => new()
    {
        ["Team1"] = team1,
        ["Team2"] = team2,
        ["DateTimeUTC"] = dateTimeUtc,
        ["BestOf"] = "5",
        ["Winner"] = winner,
        ["Team1Score"] = team1Score,
        ["Team2Score"] = team2Score,
        ["OverviewPage"] = "CBLOL/2026_Season/Split_1_Playoffs",
        ["MatchId"] = "CBLOL2026-MATCH-42",
        ["Tab"] = "Round 2",
    };

    private static string CreateCargoJson(IEnumerable<Dictionary<string, string?>> rows)
    {
        var payload = new
        {
            cargoquery = rows.Select(row => new { title = row }).ToArray(),
        };

        return JsonSerializer.Serialize(payload);
    }

    private sealed class StubLeaguepediaHandler(string matchScheduleJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.AbsoluteUri ?? string.Empty;
            var body = ResolveResponseBody(url);

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

            return Task.FromResult(response);
        }

        private string ResolveResponseBody(string url)
        {
            if (url.Contains("action=cargoquery", StringComparison.OrdinalIgnoreCase))
            {
                if (url.Contains("tables=MatchScheduleGame", StringComparison.OrdinalIgnoreCase))
                    return "{\"cargoquery\":[]}";

                if (url.Contains("tables=MatchSchedule", StringComparison.OrdinalIgnoreCase))
                    return matchScheduleJson;

                if (url.Contains("tables=Teams", StringComparison.OrdinalIgnoreCase))
                    return "{\"cargoquery\":[]}";

                return "{\"cargoquery\":[]}";
            }

            return "{}";
        }
    }
}
