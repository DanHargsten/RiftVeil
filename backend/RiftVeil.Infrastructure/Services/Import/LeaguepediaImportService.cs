using Microsoft.EntityFrameworkCore;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Import;

public class LeaguepediaImportService(LeaguepediaClient client, RiftVeilDbContext dbContext)
{
    public async Task ImportLeaguepediaAsync(string leagueName, int leagueId)
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

            // Skip if already imported
            var exists = await dbContext.Tournaments
                .AnyAsync(t => t.LiquipediaSlug == overviewPage);

            if (exists) continue;

            var startDate = ParseDate(row.GetProperty("DateStart").GetString());
            var endDateStr = row.GetProperty("Date").GetString();
            DateTimeOffset? endDate = string.IsNullOrWhiteSpace(endDateStr)
                ? null
                : ParseDate(endDateStr);

            var status = DetermineStatus(startDate, endDate);

            var tournament = new Tournament(
                leagueId: leagueId,
                name: name,
                startsAtUtc: startDate,
                endsAtUtc: endDate,
                status: status,
                liquipediaSlug: overviewPage
            );

            dbContext.Tournaments.Add(tournament);
        }

        await dbContext.SaveChangesAsync();
    }

    private static TournamentStatus DetermineStatus(DateTimeOffset start, DateTimeOffset? end)
    {
        var now = DateTimeOffset.UtcNow;

        if (end.HasValue && end.Value < now) return TournamentStatus.Finished;
        
        return start <= now
            ? TournamentStatus.Ongoing
            : TournamentStatus.Upcoming;
    }

    private static DateTimeOffset ParseDate(string? dateStr)
    {
        return string.IsNullOrEmpty(dateStr)
            ? DateTimeOffset.UtcNow
            : DateTimeOffset.Parse(dateStr + "T00:00:00Z");
    }
}