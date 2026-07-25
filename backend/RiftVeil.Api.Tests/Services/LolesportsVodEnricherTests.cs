using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;
using RiftVeil.Infrastructure.Services.Import;

namespace RiftVeil.Api.Tests.Services;

public class LolesportsVodEnricherTests
{
    [Fact]
    public async Task EnrichVodsAsync_StrictMatchWithinThreeHours_AddsVod()
    {
        var matchTimeUtc = DateTimeOffset.UtcNow.AddDays(-1);
        var dbName = $"LolesportsVodEnricherTests_Strict_{Guid.NewGuid()}";
        await using (var db = CreateDbContext(dbName))
        {
            await SeedLeagueAsync(
                db,
                matchTimeUtc,
                usePlaceholderOpponent: false,
                addAmbiguousPlaceholderMatch: false);
        }

        await using (var db = CreateDbContext(dbName))
        {
            var enricher = CreateEnricher(db, CreateHandler(matchTimeUtc.AddHours(2)));
            await enricher.EnrichVodsAsync("LEC", recentDays: 7);
        }

        await using (var verifyDb = CreateDbContext(dbName))
        {
            var game = await verifyDb.Games.SingleAsync();
            Assert.NotNull(game.VodUrl);
            Assert.Contains("youtube.com/watch?v=video-1", game.VodUrl, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task EnrichVodsAsync_FallbackWithUniquePlaceholderMatch_ReplacesOpponentAndAddsVod()
    {
        var matchTimeUtc = DateTimeOffset.UtcNow.AddDays(-1);
        var dbName = $"LolesportsVodEnricherTests_Fallback_{Guid.NewGuid()}";
        await using (var db = CreateDbContext(dbName))
        {
            await SeedLeagueAsync(
                db,
                matchTimeUtc,
                usePlaceholderOpponent: true,
                addAmbiguousPlaceholderMatch: false);
        }

        await using (var db = CreateDbContext(dbName))
        {
            var enricher = CreateEnricher(db, CreateHandler(matchTimeUtc));
            await enricher.EnrichVodsAsync("LEC", recentDays: 7);
        }

        await using (var verifyDb = CreateDbContext(dbName))
        {
            var match = await verifyDb.Matches
                .Include(storedMatch => storedMatch.Team1)
                .Include(storedMatch => storedMatch.Team2)
                .SingleAsync();

            var game = await verifyDb.Games.SingleAsync();

            Assert.Equal("MKOI", match.Team1.ShortName);
            Assert.Equal("G2", match.Team2.ShortName);
            Assert.NotNull(game.VodUrl);
            Assert.Contains("youtube.com/watch?v=video-1", game.VodUrl, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task EnrichVodsAsync_FallbackWithMultiplePlaceholderCandidates_DoesNotEnrich()
    {
        var matchTimeUtc = DateTimeOffset.UtcNow.AddDays(-1);
        var dbName = $"LolesportsVodEnricherTests_Ambiguous_{Guid.NewGuid()}";
        await using (var db = CreateDbContext(dbName))
        {
            await SeedLeagueAsync(
                db,
                matchTimeUtc,
                usePlaceholderOpponent: true,
                addAmbiguousPlaceholderMatch: true);
        }

        await using (var db = CreateDbContext(dbName))
        {
            var enricher = CreateEnricher(db, CreateHandler(matchTimeUtc));
            await enricher.EnrichVodsAsync("LEC", recentDays: 7);
        }

        await using (var verifyDb = CreateDbContext(dbName))
        {
            var matches = await verifyDb.Matches
                .Include(storedMatch => storedMatch.Team1)
                .Include(storedMatch => storedMatch.Team2)
                .OrderBy(storedMatch => storedMatch.Id)
                .ToListAsync();

            var games = await verifyDb.Games.OrderBy(game => game.Id).ToListAsync();

            Assert.Equal(2, matches.Count);
            Assert.All(matches, storedMatch =>
            {
                Assert.Equal("MKOI", storedMatch.Team1.ShortName);
                Assert.StartsWith("UNK", storedMatch.Team2.ShortName, StringComparison.OrdinalIgnoreCase);
            });
            Assert.All(games, game => Assert.True(string.IsNullOrWhiteSpace(game.VodUrl)));
        }
    }

    private static LolesportsVodEnricher CreateEnricher(RiftVeilDbContext dbContext, HttpMessageHandler handler)
    {
        var loggerFactory = LoggerFactory.Create(_ => { });
        var options = Options.Create(new LolesportsClientOptions
        {
            ApiKey = "test-key",
            MaxAttempts = 1,
            RetryDelayMilliseconds = 1,
        });

        var client = new LolesportsClient(
            new HttpClient(handler),
            options,
            loggerFactory.CreateLogger<LolesportsClient>());

        return new LolesportsVodEnricher(
            dbContext,
            client,
            loggerFactory.CreateLogger<LolesportsVodEnricher>());
    }

    private static RiftVeilDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<RiftVeilDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new RiftVeilDbContext(options);
    }

    private static async Task SeedLeagueAsync(
        RiftVeilDbContext dbContext,
        DateTimeOffset matchTimeUtc,
        bool usePlaceholderOpponent,
        bool addAmbiguousPlaceholderMatch)
    {
        var league = new League("League of Legends EMEA Championship", "LEC", "EMEA");
        dbContext.Leagues.Add(league);
        await dbContext.SaveChangesAsync();

        var mkoi = new Team("Movistar KOI", "MKOI", "EMEA");
        var g2 = new Team("G2 Esports", "G2", "EMEA");
        var unknown1 = new Team("TBD", "UNK3", "EMEA");
        var unknown2 = new Team("To Be Decided", "UNK4", "EMEA");
        dbContext.Teams.AddRange(mkoi, g2, unknown1, unknown2);
        await dbContext.SaveChangesAsync();

        var tournament = new Tournament(
            leagueId: league.Id,
            name: "LEC 2026 Spring Playoffs",
            startsAtUtc: matchTimeUtc.AddDays(-6),
            endsAtUtc: matchTimeUtc.AddDays(6),
            status: TournamentStatus.Ongoing,
            externalId: "lec-2026-spring-playoffs",
            liquipediaSlug: "LEC/2026 Season/Spring Playoffs");
        dbContext.Tournaments.Add(tournament);
        await dbContext.SaveChangesAsync();

        var opponentId = usePlaceholderOpponent ? unknown1.Id : g2.Id;
        var match = new Match(
            tournamentId: tournament.Id,
            team1Id: mkoi.Id,
            team2Id: opponentId,
            startsAtUtc: matchTimeUtc,
            bestOf: 5,
            status: MatchStatus.Finished,
            round: "Round 1",
            externalId: "LEC_MATCH_1");
        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync();

        if (addAmbiguousPlaceholderMatch)
        {
            var ambiguous = new Match(
                tournamentId: tournament.Id,
                team1Id: mkoi.Id,
                team2Id: unknown2.Id,
                startsAtUtc: matchTimeUtc.AddMinutes(15),
                bestOf: 5,
                status: MatchStatus.Finished,
                round: "Round 2",
                externalId: "LEC_MATCH_2");
            dbContext.Matches.Add(ambiguous);
            await dbContext.SaveChangesAsync();

            dbContext.Games.Add(new Game(ambiguous.Id, gameNumber: 1));
        }

        dbContext.Games.Add(new Game(match.Id, gameNumber: 1));
        await dbContext.SaveChangesAsync();
    }

    private static HttpMessageHandler CreateHandler(DateTimeOffset matchTimeUtc) =>
        new StubLolesportsHandler(
            leaguesJson:
            """
            {"data":{"leagues":[{"id":"lec-league-id","slug":"lec"}]}}
            """,
            tournamentsJson:
            $@"{{""data"":{{""leagues"":[{{""tournaments"":[{{""id"":""lec-tournament-id"",""startDate"":""{matchTimeUtc.AddDays(-6):yyyy-MM-ddTHH:mm:ssZ}"",""endDate"":""{matchTimeUtc.AddDays(6):yyyy-MM-ddTHH:mm:ssZ}""}}]}}]}}}}",
            completedEventsJson:
            $@"{{""data"":{{""schedule"":{{""events"":[{{""startTime"":""{matchTimeUtc:yyyy-MM-ddTHH:mm:ssZ}"",""match"":{{""id"":""event-match-1"",""teams"":[{{""code"":""MKOI""}},{{""code"":""G2""}}]}}}}]}}}}}}",
            eventDetailsJson:
            """
            {"data":{"event":{"match":{"games":[{"number":1,"vods":[{"provider":"youtube","parameter":"video-1","locale":"en-US","offset":42}]}]}}}}
            """);

    private sealed class StubLolesportsHandler(
        string leaguesJson,
        string tournamentsJson,
        string completedEventsJson,
        string eventDetailsJson) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var endpoint = request.RequestUri?.AbsolutePath.Split('/').LastOrDefault();
            var body = endpoint switch
            {
                "getLeagues" => leaguesJson,
                "getTournamentsForLeague" => tournamentsJson,
                "getCompletedEvents" => completedEventsJson,
                "getEventDetails" => eventDetailsJson,
                _ => "{}",
            };

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
