using RiftVeil.Domain.Entities;

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
        EnsureLeague(context, "EMEA Championship", "LEC", "EMEA", "lec");
        EnsureLeague(context, "North America Championship", "LCS", "NA", "lcs");
        EnsureLeague(context, "LoL Champions Korea", "LCK", "Korea", "lck");
        EnsureLeague(context, "LoL Pro League", "LPL", "China", "lpl");
        EnsureLeague(context, "Campeonato Brasileiro de LoL", "CBLOL", "Brazil", "cblol");
        EnsureLeague(context, "LoL Championship Pacific", "LCP", "Asia-Pacific", "lcp");
        EnsureLeague(context, "International Championship", "INTL", "Global", "international");

        context.SaveChanges();
    }

    private static void EnsureLeague(
        RiftVeilDbContext context,
        string name,
        string shortName,
        string region,
        string externalId)
    {
        if (context.Leagues.Any(league => league.ShortName == shortName))
            return;

        context.Leagues.Add(new League(name, shortName, region, externalId: externalId));
    }
}
