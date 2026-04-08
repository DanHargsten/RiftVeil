using System.Text.Json;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Enriches games with VOD links from the lolesports API.
/// Uses getCompletedEvents per tournament for full coverage (getSchedule only returns recent pages).
/// Stores all English YouTube and Twitch VODs, sets best as Game.VodUrl.
/// </summary>
public class LolesportsVodEnricher(
    RiftVeilDbContext dbContext,
    LolesportsClient client,
    ILogger<LolesportsVodEnricher> logger)
{
    private static readonly Dictionary<string, string> LeagueSlugMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LEC"] = "lec",
        ["LCS"] = "lcs",
        ["LCK"] = "lck",
    };

    public async Task EnrichVodsAsync(string leagueShortName)
    {
        logger.LogInformation("Starting VOD enrichment for {LeagueShortName}", leagueShortName);

        var league = await dbContext.Leagues
            .FirstOrDefaultAsync(l => l.ShortName.ToUpper() == leagueShortName.ToUpper());

        if (league == null)
        {
            logger.LogWarning("League {LeagueShortName} not found in database", leagueShortName);
            return;
        }

        var unenrichedCount = await dbContext.Games
            .CountAsync(g => g.Match.Tournament.LeagueId == league.Id && string.IsNullOrEmpty(g.VodUrl));

        logger.LogInformation("{Count} games without VOD for league {LeagueShortName}", unenrichedCount, leagueShortName);
        if (unenrichedCount == 0) return;

        var lolesportsLeagueId = await GetLolesportsLeagueIdAsync(leagueShortName);
        if (lolesportsLeagueId == null)
        {
            logger.LogWarning("Could not find lolesports league ID for {LeagueShortName}", leagueShortName);
            return;
        }

        var lolesportsTournaments = await GetLolesportsTournamentsAsync(lolesportsLeagueId);
        logger.LogInformation("Found {Count} lolesports tournaments for {LeagueShortName}", lolesportsTournaments.Count, leagueShortName);

        var ourTournaments = await dbContext.Tournaments
            .Where(t => t.LeagueId == league.Id)
            .Include(t => t.Matches).ThenInclude(m => m.Team1)
            .Include(t => t.Matches).ThenInclude(m => m.Team2)
            .Include(t => t.Matches).ThenInclude(m => m.Games).ThenInclude(g => g.Vods)
            .ToListAsync();

        int totalEnriched = 0;

        foreach (var lolesportsTournament in lolesportsTournaments)
        {
            if (!lolesportsTournament.TryGetProperty("id", out var tournamentIdEl)) continue;
            var tournamentId = tournamentIdEl.GetString();
            if (string.IsNullOrEmpty(tournamentId)) continue;

            logger.LogInformation("Fetching completed events for tournament {TournamentId}", tournamentId);

            List<JsonElement> events;
            try
            {
                var completedJson = await client.CallAsync("getCompletedEvents",
                    new Dictionary<string, string> { ["tournamentId"] = tournamentId });

                events = completedJson.RootElement
                    .GetProperty("data").GetProperty("schedule").GetProperty("events")
                    .EnumerateArray().ToList();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to get completed events for tournament {TournamentId}", tournamentId);
                continue;
            }

            logger.LogInformation("{Count} completed events for tournament {TournamentId}", events.Count, tournamentId);

            if (events.Count > 0)
            {
                var first = events[0];
                logger.LogDebug("First event JSON (truncated): {Snippet}",
                    first.GetRawText()[..Math.Min(500, first.GetRawText().Length)]);

                var firstMatch = ourTournaments.SelectMany(t => t.Matches).FirstOrDefault();
                if (firstMatch != null)
                {
                    logger.LogDebug("First DB match: {Team1} vs {Team2} @ {StartsAt:yyyy-MM-dd HH:mm}",
                        firstMatch.Team1.ShortName, firstMatch.Team2.ShortName, firstMatch.StartsAtUtc);
                }
            }

            int enrichedInTournament = 0;

            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];

                if (!ev.TryGetProperty("startTime", out var startTimeEl)) continue;
                if (!DateTimeOffset.TryParse(startTimeEl.GetString(), out var evTime)) continue;

                if (!ev.TryGetProperty("match", out var matchEl)) continue;
                if (!matchEl.TryGetProperty("teams", out var teamsEl)) continue;

                var codes = teamsEl.EnumerateArray()
                    .Select(t => t.TryGetProperty("code", out var c) ? c.GetString()?.ToUpperInvariant().Trim() : null)
                    .OfType<string>()
                    .ToArray();

                if (codes.Length < 2) continue;

                if (enrichedInTournament == 0 && i < 3)
                {
                    logger.LogDebug("Event: {Code1} vs {Code2} @ {Time:yyyy-MM-dd HH:mm}", codes[0], codes[1], evTime);

                    foreach (var tournament in ourTournaments)
                    {
                        foreach (var m in tournament.Matches.Take(3))
                        {
                            var t1 = m.Team1.ShortName.ToUpperInvariant().Trim();
                            var t2 = m.Team2.ShortName.ToUpperInvariant().Trim();
                            logger.LogDebug("DB match: {T1} vs {T2} @ {Time:yyyy-MM-dd HH:mm}", t1, t2, m.StartsAtUtc);
                        }
                        break;
                    }
                }

                Match? ourMatch = null;
                foreach (var tournament in ourTournaments)
                {
                    ourMatch = tournament.Matches.FirstOrDefault(m =>
                    {
                        var t1 = m.Team1.ShortName.ToUpperInvariant().Trim();
                        var t2 = m.Team2.ShortName.ToUpperInvariant().Trim();

                        var teamsMatch = codes.Contains(t1) && codes.Contains(t2);
                        var timeClose = Math.Abs((m.StartsAtUtc - evTime).TotalMinutes) < 120;
                        return teamsMatch && timeClose;
                    });
                    if (ourMatch != null) break;
                }

                if (ourMatch == null) continue;

                var gamesNeedingVods = ourMatch.Games.Where(g => string.IsNullOrEmpty(g.VodUrl)).ToList();
                if (gamesNeedingVods.Count == 0) continue;

                if (!matchEl.TryGetProperty("id", out var eventIdEl)) continue;
                var eventId = eventIdEl.GetString();
                if (string.IsNullOrEmpty(eventId)) continue;

                JsonDocument details;
                try
                {
                    details = await client.CallAsync("getEventDetails",
                        new Dictionary<string, string> { ["id"] = eventId });
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "getEventDetails failed for event {EventId}", eventId);
                    continue;
                }

                if (!details.RootElement.TryGetProperty("data", out var dataEl)) continue;
                if (!dataEl.TryGetProperty("event", out var eventEl)) continue;
                if (!eventEl.TryGetProperty("match", out var detailMatchEl)) continue;
                if (!detailMatchEl.TryGetProperty("games", out var detailGamesEl)) continue;

                var detailGames = detailGamesEl.EnumerateArray().ToList();

                foreach (var game in gamesNeedingVods)
                {
                    if (EnrichGameVods(game, detailGames))
                        enrichedInTournament++;
                }

                await Task.Delay(500);
            }

            totalEnriched += enrichedInTournament;
            logger.LogInformation("Enriched {Count} VODs for tournament {TournamentId}", enrichedInTournament, tournamentId);

            if (enrichedInTournament > 0)
                await dbContext.SaveChangesAsync();

            await Task.Delay(2000);
        }

        logger.LogInformation("VOD enrichment finished: {Total} VODs total for {LeagueShortName}", totalEnriched, leagueShortName);
    }

    /// <summary>
    /// Extracts all English VODs for a game and sets the best one as default VodUrl.
    /// Returns true if at least one VOD was added.
    /// </summary>
    private bool EnrichGameVods(Game game, List<JsonElement> detailGames)
    {
        var targetGame = detailGames.FirstOrDefault(g =>
            g.TryGetProperty("number", out var numEl) && numEl.GetInt32() == game.GameNumber);

        if (targetGame.ValueKind == JsonValueKind.Undefined || !targetGame.TryGetProperty("vods", out var vodsEl))
            return false;

        var vods = vodsEl.EnumerateArray().ToList();
        if (vods.Count == 0) return false;

        bool any = false;
        string? bestUrl = null;
        VodProvider bestProvider = VodProvider.Twitch;

        foreach (var vod in vods)
        {
            var providerStr = vod.TryGetProperty("provider", out var p) ? p.GetString() : null;
            var videoId = vod.TryGetProperty("parameter", out var param) ? param.GetString() : null;
            var locale = vod.TryGetProperty("locale", out var l) ? l.GetString() : null;

            if (string.IsNullOrEmpty(providerStr) || string.IsNullOrEmpty(videoId)) continue;

            VodProvider provider;
            if (providerStr == "youtube") provider = VodProvider.YouTube;
            else if (providerStr == "twitch") provider = VodProvider.Twitch;
            else continue;

            if (locale != "en-US") continue;

            var offset = vod.TryGetProperty("offset", out var o) && o.ValueKind == JsonValueKind.Number
                ? o.GetInt32() : 0;

            string url;
            if (provider == VodProvider.YouTube)
            {
                url = $"https://www.youtube.com/watch?v={videoId}";
                if (offset > 10) url += $"&t={offset}s";
            }
            else
            {
                url = $"https://www.twitch.tv/videos/{videoId}";
                if (offset > 10) url += $"?t={offset}s";
            }

            var added = game.AddGameVod(provider, url, locale, videoId, offset);
            if (added == null) continue;

            any = true;
            logger.LogDebug("Game {GameNumber}: {Provider} | {Locale} | {VideoId}", game.GameNumber, provider, locale, videoId);

            if (bestUrl == null || (provider == VodProvider.YouTube && bestProvider != VodProvider.YouTube))
            {
                bestUrl = url;
                bestProvider = provider;
            }
        }

        if (bestUrl != null && string.IsNullOrEmpty(game.VodUrl))
        {
            game.SetVodUrl(bestUrl);
        }

        return any;
    }

    private async Task<string?> GetLolesportsLeagueIdAsync(string shortName)
    {
        if (!LeagueSlugMap.TryGetValue(shortName, out var slug)) return null;

        var leaguesJson = await client.CallAsync("getLeagues");
        var leagues = leaguesJson.RootElement
            .GetProperty("data").GetProperty("leagues")
            .EnumerateArray();

        foreach (var league in leagues)
        {
            if (league.TryGetProperty("slug", out var slugEl) && slugEl.GetString() == slug)
                return league.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
        }

        return null;
    }

    private async Task<List<JsonElement>> GetLolesportsTournamentsAsync(string leagueId)
    {
        var json = await client.CallAsync("getTournamentsForLeague",
            new Dictionary<string, string> { ["leagueId"] = leagueId });

        var tournaments = new List<JsonElement>();

        if (!json.RootElement.TryGetProperty("data", out var dataEl)) return tournaments;
        if (!dataEl.TryGetProperty("leagues", out var leaguesEl)) return tournaments;

        foreach (var league in leaguesEl.EnumerateArray())
        {
            if (!league.TryGetProperty("tournaments", out var tournamentsEl)) continue;
            foreach (var t in tournamentsEl.EnumerateArray())
                tournaments.Add(t.Clone());
        }

        return tournaments;
    }
}
