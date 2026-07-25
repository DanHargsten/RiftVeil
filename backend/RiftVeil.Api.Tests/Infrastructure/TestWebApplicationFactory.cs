using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Api.Tests.Infrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RiftVeilTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Keep development-only database initialization out of integration tests.
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Lolesports:ApiKey"] = "test-key",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the real database context
            services.RemoveAll(typeof(DbContextOptions<RiftVeilDbContext>));

            // Add in-memory database
            services.AddDbContext<RiftVeilDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Ensure database is created and seeded
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<RiftVeilDbContext>();

            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            SeedTestData(context);
        });

        // Avoid the Windows Event Log provider in test runs. It can require elevated
        // permissions and otherwise masks the assertion that actually failed.
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
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

        // Seed teams
        var fnc = new Team("Fnatic", "FNC", "Europe");
        var g2 = new Team("G2 Esports", "G2", "Europe");
        var kc = new Team("Karmine Corp", "KC", "Europe");
        var mkoi = new Team("MAD Lions KOI", "MKOI", "Europe");
        var bds = new Team("Team BDS", "BDS", "Europe");
        var sk = new Team("SK Gaming", "SK", "Europe");
        var t1 = new Team("T1", "T1", "Korea");
        var gen = new Team("Gen.G", "GEN", "Korea");

        context.Teams.AddRange(fnc, g2, kc, mkoi, bds, sk, t1, gen);
        context.SaveChanges();

        // Seed leagues
        var lec = new League("League of Legends EMEA Championship", "LEC", "EMEA",
            "https://example.com/lec-png", "lec");
        var lcs = new League("League of Legends North America Championship", "LCS", "NA",
            "https://example.com/lcs.png", "lcs");
        var intl = new League("League of Legends International Championship", "INTL", "Global",
            externalId: "international");

        context.Leagues.AddRange(lec, lcs, intl);
        context.SaveChanges();

        // Seed tournaments
        var lecSpring2026 = new Tournament(lec.Id, "LEC Spring 2026", now.AddDays(-14), now.AddDays(30),
            TournamentStatus.Ongoing, externalId: "lec-spring-2026", liquipediaSlug: "LEC/2026_Season/Spring_Season");
        var lcsSpring2026 = new Tournament(lcs.Id, "LCS Spring 2026", now.AddDays(-10), now.AddDays(35),
            TournamentStatus.Ongoing, externalId: "lcs-spring-2026", liquipediaSlug: "LCS/2026_Season/Spring_Season");
        var worlds2025 = new Tournament(intl.Id, "Worlds 2025", now.AddMonths(-3), now.AddMonths(-2),
            TournamentStatus.Finished, externalId: "worlds-2025", liquipediaSlug: "Worlds_2025");

        context.Tournaments.AddRange(lecSpring2026, lcsSpring2026, worlds2025);
        context.SaveChanges();

        // Seed matches
        var scheduledToday = new Match(lecSpring2026.Id, fnc.Id, g2.Id, now.AddHours(6), 1);
        var scheduledTomorrow = new Match(lecSpring2026.Id, kc.Id, mkoi.Id, now.AddHours(20), 1);
        var scheduledNextWeek = new Match(lecSpring2026.Id, bds.Id, sk.Id, now.AddDays(6).AddHours(2), 1);

        var finishedMatch = new Match(worlds2025.Id, t1.Id, gen.Id, now.AddMonths(-3).AddDays(5), 5,
            MatchStatus.Finished, "worlds-2025-final");
        finishedMatch.MarkFinished(finishedMatch.StartsAtUtc, finishedMatch.StartsAtUtc.AddHours(4), 3, 2,
            "https://example.com/vod/worlds-2025-final");

        context.Matches.AddRange(scheduledToday, scheduledTomorrow, scheduledNextWeek, finishedMatch);
        context.SaveChanges();
    }
}
