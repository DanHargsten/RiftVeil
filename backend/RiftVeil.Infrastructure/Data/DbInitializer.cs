using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Infrastructure.Data;

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

        // --- Leagues ---
        var lec = new League(
            name: "League of Legends EMEA Championship",
            shortName: "LEC",
            region: "EMEA",
            logoUrl: null,
            externalId: "lec"
        );

        var lcs = new League(
            name: "League of Legends North America Championship",
            shortName: "LCS",
            region: "NA",
            logoUrl: null,
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

        // --- Tournaments ---
        var worlds2025 = new Tournament(
            leagueId: intl.Id,
            name: "Worlds 2025",
            startsAtUtc: now.AddMonths(-3),
            status: TournamentStatus.Finished,
            endsAtUtc: now.AddMonths(-2),
            externalId: "worlds-2025",
            liquipediaSlug: "Worlds_2025"
        );

        var lecSpring2026 = new Tournament(
            leagueId: lec.Id,
            name: "LEC Spring 2026",
            startsAtUtc: now.AddDays(-14),
            status: TournamentStatus.Ongoing,
            endsAtUtc: null,
            externalId: "lec-spring-2026",
            liquipediaSlug: "LEC/2026_Season/Spring_Season"
        );

        context.Tournaments.AddRange(worlds2025, lecSpring2026);
        context.SaveChanges();

        // --- Matches ---
        // 2 finished matches (with scores + VOD)
        var finished1 = new Match(
            tournamentId: worlds2025.Id,
            team1Name: "T1",
            team2Name: "G2 Esports",
            startsAtUtc: now.AddMonths(-3).AddDays(2),
            bestOf: 5,
            status: MatchStatus.Finished,
            externalId: "worlds-2025-final",
            vodUrl: null
        );

        finished1.MarkFinished(
            startedAtUtc: finished1.StartsAtUtc,
            finishedAtUtc: finished1.StartsAtUtc.AddHours(4),
            team1Score: 3,
            team2Score: 2,
            vodUrl: "https://example.com/vod/worlds-2025-final"
        );

        var finished2 = new Match(
            tournamentId: worlds2025.Id,
            team1Name: "Gen.G",
            team2Name: "Fnatic",
            startsAtUtc: now.AddMonths(-3).AddDays(1),
            bestOf: 3,
            status: MatchStatus.Finished,
            externalId: "worlds-2025-semi-1",
            vodUrl: null
        );

        finished2.MarkFinished(
            startedAtUtc: finished2.StartsAtUtc,
            finishedAtUtc: finished2.StartsAtUtc.AddHours(2),
            team1Score: 2,
            team2Score: 1,
            vodUrl: "https://example.com/vod/worlds-2025-semi-1"
        );

        // 3 scheduled matches (today/tomorrow/next week)
        var scheduledToday = new Match(
            tournamentId: lecSpring2026.Id,
            team1Name: "Karmine Corp",
            team2Name: "MAD Lions KOI",
            startsAtUtc: now.AddHours(6),
            bestOf: 1,
            status: MatchStatus.Scheduled,
            externalId: "lec-spring-2026-week-1-match-1"
        );

        var scheduledTomorrow = new Match(
            tournamentId: lecSpring2026.Id,
            team1Name: "Fnatic",
            team2Name: "G2 Esports",
            startsAtUtc: now.AddDays(1).AddHours(4),
            bestOf: 1,
            status: MatchStatus.Scheduled,
            externalId: "lec-spring-2026-week-1-match-2"
        );

        var scheduledNextWeek = new Match(
            tournamentId: lecSpring2026.Id,
            team1Name: "Team BDS",
            team2Name: "SK Gaming",
            startsAtUtc: now.AddDays(7).AddHours(2),
            bestOf: 1,
            status: MatchStatus.Scheduled,
            externalId: "lec-spring-2026-week-2-match-1"
        );

        context.Matches.AddRange(finished1, finished2, scheduledToday, scheduledTomorrow, scheduledNextWeek);
        context.SaveChanges();
    }
}
