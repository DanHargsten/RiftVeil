using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Infrastructure.Data;

/// <summary>
/// Seeds a minimal dataset for local development and demos.
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initializes the database with default data.
    /// </summary>
    /// <param name="context">The database context.</param>
    public static void Initialize(RiftVeilDbContext context)
    {
        if (context.Leagues.Any())
            return;

        var now = DateTimeOffset.UtcNow;

        // Teams
        var t1 = new Team("T1", "T1", "Korea");
        var g2 = new Team("G2 Esports", "G2", "Europe");
        var gen = new Team("Generation Gaming", "Gen", "Korea");
        var fnc = new Team("Fnatic", "FNC", "Europe");
        var kc = new Team("Karmine Corp", "KC", "Europe");
        var mkoi = new Team("Movistar KOI", "KOI", "Europe");
        var bds = new Team("Team BDS", "BDS", "Europe");
        var sk = new Team("SK Gaming", "Sk", "Europe");
        
        context.Teams.AddRange(t1, g2, gen, fnc, kc, mkoi, bds, sk);
        context.SaveChanges();
        
        // Leagues
        var lec = new League("EMEA Championship", "LEC", "EMEA", externalId: "lec");
        var lcs = new League("North America Championship", "LCS", "NA",  externalId: "lcs");
        var intl = new League("International Championship", "INTL", "Global",  externalId: "international");
        
        context.Leagues.AddRange(lec, lcs, intl);
        context.SaveChanges();
        
        // Tournaments
        var worlds2025 = new Tournament(intl.Id, "Worlds 2025", now.AddMonths(-3), now.AddMonths(-2),
            TournamentStatus.Finished, "Finals", "worlds-2025", "Worlds_2025");

        var lecSpring2026 = new Tournament(lec.Id, "LEC Spring 2026", now.AddDays(-14), null,
            TournamentStatus.Ongoing, "Playoffs", "lec-spring-2026", "LEC/2026_Season/Spring_Season");

        context.Tournaments.AddRange(worlds2025, lecSpring2026);
        context.SaveChanges();
        
        // Matches
        var finished1 = new Match(worlds2025.Id, t1.Id, g2.Id, now.AddMonths(-3).AddDays(2), 5,
            MatchStatus.Finished, "worlds-2025-final");
        finished1.MarkFinished(finished1.StartsAtUtc, finished1.StartsAtUtc.AddHours(4), 3, 2,
            "https://example.com/vod/worlds-2025-final");

        var finished2 = new Match(worlds2025.Id, gen.Id, fnc.Id, now.AddMonths(-3).AddDays(1), 3,
            MatchStatus.Finished, "worlds-2025-semi-1");
        finished2.MarkFinished(finished2.StartsAtUtc, finished2.StartsAtUtc.AddHours(2), 2, 1,
            "https://example.com/vod/worlds-2025-semi-1");

        var scheduledToday = new Match(lecSpring2026.Id, kc.Id, mkoi.Id, now.AddHours(6), 1);
        var scheduledTomorrow = new Match(lecSpring2026.Id, fnc.Id, g2.Id, now.AddDays(1).AddHours(4), 1);
        var scheduledNextWeek = new Match(lecSpring2026.Id, bds.Id, sk.Id, now.AddDays(7).AddHours(2), 1);

        context.Matches.AddRange(finished1, finished2, scheduledToday, scheduledTomorrow, scheduledNextWeek);
        context.SaveChanges();
        
        // Games
        context.Games.AddRange(
            new Game(finished1.Id, 1, winningTeam: 1, vodUrl: "https://example.com/vod/worlds-final-g1"),
            new Game(finished1.Id, 2, winningTeam: 2, vodUrl: "https://example.com/vod/worlds-final-g2"),
            new Game(finished1.Id, 3, winningTeam: 1, vodUrl: "https://example.com/vod/worlds-final-g3"),
            new Game(finished1.Id, 4, winningTeam: 2, vodUrl: "https://example.com/vod/worlds-final-g4"),
            new Game(finished1.Id, 5, winningTeam: 1, vodUrl: "https://example.com/vod/worlds-final-g5")
        );

        context.Games.AddRange(
            new Game(finished2.Id, 1, winningTeam: 1, vodUrl: "https://example.com/vod/worlds-semi1-g1"),
            new Game(finished2.Id, 2, winningTeam: 2, vodUrl: "https://example.com/vod/worlds-semi1-g2"),
            new Game(finished2.Id, 3, winningTeam: 1, vodUrl: "https://example.com/vod/worlds-semi1-g3")
        );

        context.SaveChanges();
    }
}