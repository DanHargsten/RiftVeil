using Microsoft.EntityFrameworkCore;
using RiftVeil.Domain.Entities;
using RiftVeil.Domain.Enums;
using System.Text.Json;
using RiftVeil.Infrastructure.Data;

namespace RiftVeil.Infrastructure.Services.Import;

/// <summary>
/// Enriches games with VOD links from the lolesports API.
/// Uses getCompletedEvents per tournament for full coverage (getSchedule only returns recent pages).
/// Stores all English YouTube and Twitch VODs, sets best as Game.VodUrl.
/// </summary>
public class LolesportsVodEnricher(RiftVeilDbContext dbContext, LolesportsClient client)
{
    private static readonly Dictionary<string, string> LeagueSlugMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["LEC"] = "lec",
        ["LCS"] = "lcs",
        ["LCK"] = "lck",
    };

    public async Task EnrichVodsAsync(string leagueShortName)
    {
        Console.WriteLine($"Starting VOD enrichment for {leagueShortName}");

        var league = await dbContext.Leagues
            .FirstOrDefaultAsync(l => l.ShortName.ToUpper() == leagueShortName.ToUpper());

        if (league == null)
        {
            Console.WriteLine($"League '{leagueShortName}' not found in DB");
            return;
        }

        var unenrichedCount = await dbContext.Games
            .CountAsync(g => g.Match.Tournament.LeagueId == league.Id && string.IsNullOrEmpty(g.VodUrl));

        Console.WriteLine($"  {unenrichedCount} games without VOD");
        if (unenrichedCount == 0) return;

        var lolesportsLeagueId = await GetLolesportsLeagueIdAsync(leagueShortName);
        if (lolesportsLeagueId == null)
        {
            Console.WriteLine($"  Could not find lolesports league ID for '{leagueShortName}'");
            return;
        }

        var lolesportsTournaments = await GetLolesportsTournamentsAsync(lolesportsLeagueId);
        Console.WriteLine($"  Found {lolesportsTournaments.Count} lolesports tournaments");

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

            Console.WriteLine($"  Fetching completed events for tournament {tournamentId}...");

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
                Console.WriteLine($"    Failed: {ex.Message}");
                continue;
            }

            Console.WriteLine($"    {events.Count} completed events");
            
            
            
            
            if (events.Count > 0)
            {
                var first = events[0];
                Console.WriteLine($"    [DEBUG] First event JSON: {first.GetRawText()[..Math.Min(500, first.GetRawText().Length)]}");
    
                // Also dump first DB match for this league
                var firstMatch = ourTournaments.SelectMany(t => t.Matches).FirstOrDefault();
                if (firstMatch != null)
                {
                    Console.WriteLine($"    [DEBUG] First DB match: {firstMatch.Team1?.ShortName} vs {firstMatch.Team2?.ShortName} @ {firstMatch.StartsAtUtc:yyyy-MM-dd HH:mm}");
                }
            }
            
            
            

            int enrichedInTournament = 0;

            foreach (var ev in events)
            {
                if (!ev.TryGetProperty("startTime", out var startTimeEl)) continue;
                if (!DateTimeOffset.TryParse(startTimeEl.GetString(), out var evTime)) continue;

                if (!ev.TryGetProperty("match", out var matchEl)) continue;
                if (!matchEl.TryGetProperty("teams", out var teamsEl)) continue;

                var codes = teamsEl.EnumerateArray()
                    .Select(t => t.TryGetProperty("code", out var c) ? c.GetString()?.ToUpperInvariant().Trim() : null)
                    .Where(c => c != null)
                    .ToArray();

                if (codes.Length < 2) continue;
                
                
                
                // Debug: log first few events to see what we're matching against
                if (enrichedInTournament == 0 && events.IndexOf(ev) < 3)
                {
                    Console.WriteLine($"    [DEBUG] Event: {codes[0]} vs {codes[1]} @ {evTime:yyyy-MM-dd HH:mm}");
    
                    // Show what's in our DB for comparison
                    foreach (var tournament in ourTournaments)
                    {
                        foreach (var m in tournament.Matches.Take(3))
                        {
                            var t1 = m.Team1?.ShortName?.ToUpperInvariant().Trim();
                            var t2 = m.Team2?.ShortName?.ToUpperInvariant().Trim();
                            Console.WriteLine($"    [DEBUG] DB match: {t1} vs {t2} @ {m.StartsAtUtc:yyyy-MM-dd HH:mm}");
                        }
                        break; // only first tournament
                    }
                }
                
                

                Match? ourMatch = null;
                foreach (var tournament in ourTournaments)
                {
                    ourMatch = tournament.Matches.FirstOrDefault(m =>
                    {
                        var t1 = m.Team1?.ShortName?.ToUpperInvariant().Trim();
                        var t2 = m.Team2?.ShortName?.ToUpperInvariant().Trim();
                        if (t1 == null || t2 == null) return false;

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
                catch
                {
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
            Console.WriteLine($"    Enriched {enrichedInTournament} VODs");

            if (enrichedInTournament > 0)
                await dbContext.SaveChangesAsync();

            await Task.Delay(2000);
        }

        Console.WriteLine($"\nDone! Enriched {totalEnriched} VODs total for {leagueShortName}");
    }

    /// <summary>
    /// Extracts all English VODs for a game and sets the best one as default VodUrl.
    /// Returns true if at least one VOD was added.
    /// </summary>
    private static bool EnrichGameVods(Game game, List<JsonElement> detailGames)
    {
        var targetGame = detailGames.FirstOrDefault(g =>
            g.TryGetProperty("number", out var numEl) && numEl.GetInt32() == game.GameNumber);

        if (targetGame.ValueKind == JsonValueKind.Undefined) return false;
        if (!targetGame.TryGetProperty("vods", out var vodsEl)) return false;

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

            // Map string to enum
            VodProvider provider;
            if (providerStr == "youtube") provider = VodProvider.YouTube;
            else if (providerStr == "twitch") provider = VodProvider.Twitch;
            else continue;

            // Only official English (en-US) VODs — other en-* locales are often
            // hobby/community streams with incorrect locale tags in Lolesports data.
            if (locale != "en-US") continue;

            var offset = vod.TryGetProperty("offset", out var o) && o.ValueKind == JsonValueKind.Number
                ? o.GetInt32() : 0;

            // Build URL
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
            if (added == null) continue; // duplicate

            any = true;
            Console.WriteLine($"    └─ Game {game.GameNumber}: {provider} | {locale} | {videoId}");

            // Track best URL: YouTube preferred over Twitch
            if (bestUrl == null || (provider == VodProvider.YouTube && bestProvider != VodProvider.YouTube))
            {
                bestUrl = url;
                bestProvider = provider;
            }
        }

        // Set the preferred VOD as the quick-access VodUrl
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