using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Shared tournament selection for scoped import jobs (ongoing, recent window, all).
/// </summary>
public static class ImportTournamentFilter
{
    public const int DefaultRecentDays = 7;

    /// <summary>
    /// Tournaments whose schedule overlaps the last <paramref name="recentDays"/> days
    /// (includes ongoing and just-finished events).
    /// </summary>
    public static IQueryable<Tournament> WhereRecent(
        IQueryable<Tournament> query,
        DateTimeOffset utcNow,
        int recentDays = DefaultRecentDays)
    {
        var cutoff = utcNow.AddDays(-recentDays);
        return query.Where(tournament =>
            tournament.LiquipediaSlug != null
            && tournament.StartsAtUtc <= utcNow
            && (tournament.EndsAtUtc == null || tournament.EndsAtUtc >= cutoff));
    }

    public static IQueryable<Tournament> WhereOngoing(IQueryable<Tournament> query, DateTimeOffset utcNow) =>
        query.Where(tournament =>
            tournament.LiquipediaSlug != null
            && tournament.StartsAtUtc <= utcNow
            && (tournament.EndsAtUtc == null || tournament.EndsAtUtc >= utcNow));

    public static IQueryable<Tournament> WhereOngoingByStatus(IQueryable<Tournament> query) =>
        query.Where(tournament =>
            tournament.LiquipediaSlug != null
            && tournament.Status == TournamentStatus.Ongoing);
}
