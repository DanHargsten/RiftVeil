using Microsoft.EntityFrameworkCore;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Imports tournament data from Leaguepedia into the local database.
/// </summary>
public class LeaguepediaImportService(LeaguepediaClient client, RiftVeilDbContext dbContext)
{
    /// <summary>
    /// Imports tournaments for the given league from Leaguepedia.
    /// Skips tournaments that already exist (matched by Liquipedia slug).
    /// </summary>
    /// <param name="leagueName">League name as used in Leaguepedia.</param>
    /// <param name="leagueId">Local league ID to associate tournaments with.</param>
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

    /// <summary>
    /// Determines tournament status from start and end dates.
    /// </summary>
    private static TournamentStatus DetermineStatus(DateTimeOffset start, DateTimeOffset? end)
    {
        var now = DateTimeOffset.UtcNow;

        if (end.HasValue && end.Value < now) return TournamentStatus.Finished;
        
        return start <= now
            ? TournamentStatus.Ongoing
            : TournamentStatus.Upcoming;
    }

    /// <summary>
    /// Parses Leaguepedia date string (YYYY-MM-DD) to UTC.
    /// Uses UtcNow as fallback when date is missing (avoids invalid data).
    /// </summary>
    private static DateTimeOffset ParseDate(string? dateStr)
    {
        return string.IsNullOrEmpty(dateStr)
            ? DateTimeOffset.UtcNow  // Fallback when Leaguepedia returns empty date
            : DateTimeOffset.Parse(dateStr + "T00:00:00Z");
    }
}