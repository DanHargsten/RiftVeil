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

        // Leagues
        var lec = new League("EMEA Championship", "LEC", "EMEA", externalId: "lec");
        var lcs = new League("North America Championship", "LCS", "NA", externalId: "lcs");
        var intl = new League("International Championship", "INTL", "Global", externalId: "international");

        context.Leagues.AddRange(lec, lcs, intl);
        context.SaveChanges();
    }
}
