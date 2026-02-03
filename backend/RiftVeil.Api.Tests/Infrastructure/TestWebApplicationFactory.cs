using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RiftVeil.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using System.Text.Json;

namespace RiftVeil.Api.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RiftVeilTestDb_{Guid.NewGuid()}";
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the real database context
            services.RemoveAll(typeof(DbContextOptions<RiftVeilDbContext>));

            // Add in-memory database
            services.AddDbContext<RiftVeilDbContext>(options =>
            {
                options.UseInMemoryDatabase("RiftVeilTestDb");
            });

            // Ensure database is created and seeded
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RiftVeilDbContext>();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            SeedTestData(context);
        });
    }

    public new HttpClient CreateClient()
    {
        var client = base.CreateClient();
        return client;
    }

    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private static void SeedTestData(RiftVeilDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        
        // Seed leagues
        var lec = new League(
            name: "League of Legends EMEA Championship",
            shortName: "LEC",
            region: "EMEA",
            logoUrl: "https://example.com/lec-png",
            externalId: "lec"
        );
        
        var lcs = new League(
            name: "League of Legends North America Championship",
            shortName: "LCS",
            region: "NA",
            logoUrl: "https://example.com/lcs.png",
            externalId: "lcs"
        );

        var intl = new League(
            name: "League of Legends International Championship",
            shortName: "INTL",
            region: "Global",
            logoUrl: null,
            externalId: "international"
        );

        context.Leagues.AddRange(lec, lcs, intl);
        context.SaveChanges();
        
        // Seed tournaments
        var lecSpring2026 = new Tournament(
            leagueId: lec.Id,
            name: "LEC Spring 2026",
            startsAtUtc: now.AddDays(-14),
            endsAtUtc: now.AddDays(30),
            status: TournamentStatus.Ongoing,
            externalId: "lec-spring-2026",
            liquipediaSlug: "LEC/2026_Season/Spring_Season"
        );

        var lcsSpring2026 = new Tournament(
            leagueId: lcs.Id,
            name: "LCS Spring 2026",
            startsAtUtc: now.AddDays(-10),
            endsAtUtc: now.AddDays(35),
            status: TournamentStatus.Ongoing,
            externalId: "lcs-spring-2026",
            liquipediaSlug: "LCS/2026_Season/Spring_Season"
        );

        var worlds2025 = new Tournament(
            leagueId: intl.Id,
            name: "Worlds 2025",
            startsAtUtc: now.AddMonths(-3),
            endsAtUtc: now.AddMonths(-2),
            status: TournamentStatus.Finished,
            externalId: "worlds-2025",
            liquipediaSlug: "Worlds_2025"
        );

        context.Tournaments.AddRange(lecSpring2026, lcsSpring2026, worlds2025);
        context.SaveChanges();
        
        // Seed matches
        var scheduledToday = new Match(
            tournamentId: lecSpring2026.Id,
            team1Name: "Fnatic",
            team2Name: "G2 Esports",
            team1ShortName: "FNC",
            team2ShortName: "G2",
            startsAtUtc: now.AddHours(6),
            bestOf: 1,
            status: MatchStatus.Scheduled,
            externalId: "lec-spring-2026-match-1",
            vodUrl: null
        );

        var scheduledTomorrow = new Match(
            tournamentId: lecSpring2026.Id,
            team1Name: "Karmine Corp",
            team2Name: "MAD Lions KOI",
            team1ShortName: "KC",
            team2ShortName: "MAD", 
            startsAtUtc: now.AddHours(20),
            bestOf: 1,
            status: MatchStatus.Scheduled,
            externalId: "lec-spring-2026-match-2",
            vodUrl: null
        );

        var scheduledNextWeek = new Match(
            tournamentId: lecSpring2026.Id,
            team1Name: "Team BDS",
            team2Name: "SK Gaming",
            team1ShortName: "BDS",
            team2ShortName: "SK",
            startsAtUtc: now.AddDays(6).AddHours(2),
            bestOf: 1,
            status: MatchStatus.Scheduled,
            externalId: "lec-spring-2026-match-3",
            vodUrl: null
        );

        var finishedMatch = new Match(
            tournamentId: worlds2025.Id,
            team1Name: "T1",
            team2Name: "Gen.G",
            team1ShortName: "T1",
            team2ShortName: "GEN",     
            startsAtUtc: now.AddMonths(-3).AddDays(5),
            bestOf: 5,
            status: MatchStatus.Finished,
            externalId: "worlds-2025-final",
            vodUrl: null
        );
        finishedMatch.MarkFinished(
            startedAtUtc: finishedMatch.StartsAtUtc,
            finishedAtUtc: finishedMatch.StartsAtUtc.AddHours(4),
            team1Score: 3,
            team2Score: 2,
            vodUrl: "https://example.com/vod/worlds-2025-final"
        );

        context.Matches.AddRange(scheduledToday, scheduledTomorrow, scheduledNextWeek, finishedMatch);
        context.SaveChanges();
    }
}