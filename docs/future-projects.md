# Future Projects

A backlog of larger, scoped pieces of work that are intentionally **not** implemented yet but are
worth designing now so we can pick them up cleanly later. Items here are ordered by impact, not
urgency.

---

## 1. Background auto-import (cron-like sync)

### Problem

Imports today are triggered manually via `/admin` (or `POST /api/import/...`). For production, we
want the user-facing site to always reflect the latest results without anyone clicking buttons.
Match-detail pages should already have player/team/draft stats by the time someone opens them — so
the **15 s+** of Leaguepedia work is never on the user's critical path.

### Goal

When a user opens the home page or a match-detail page in production, **everything is already
imported**. Manual `/admin` triggers stay available for backfills and one-off corrections.

### Proposed architecture

- A single `IHostedService` (no Hangfire / Quartz — overkill at this scale) using `PeriodicTimer`s.
- Three independent loops with different cadences:

| Loop | Cadence | What it does |
|---|---|---|
| **Tournaments** | once per day | `ImportTournamentsAsync` for each league. Picks up new splits / playoffs. |
| **Matches** | every 30 min, or every 5 min during a known match window | `ImportOngoingMatchesAsync` per league. Updates `Status` from `Scheduled` → `Live` → `Finished`. **Replaces** the time-window heuristic in `MatchProjections.LiveWindowMinutesPerGame` (see #6). |
| **Game details** | every 5 min | Game-detail import for any `Game` whose parent `Match.Status` recently flipped to `Finished` and that has no detail rows yet. |

A `Game.DetailsImportedAtUtc` (or just "exists row in `GamePlayerStats`") flag is used to skip
already-imported games.

### Configuration

- Toggle the whole thing off via env / `appsettings`:

```jsonc
"AutoImport": {
  "Enabled": true,
  "TournamentsCadenceMinutes": 1440,
  "MatchesCadenceMinutes": 30,
  "GameDetailsCadenceMinutes": 5
}
```

- Default `Enabled = false`, set `true` only in production / staging. Dev keeps `/admin`.

### UX once it's live

- `MatchDetail` page calls `GET /api/games/{id}/details`. If rows exist → render. If not (rare,
  edge case where someone opens within the 5-min window after a match ends) → show a small
  "Importing details…" placeholder and poll every 10 s until ready. No big spinner, no blocking.

### Out of scope for v1

- SignalR/WebSocket push when import finishes (poll is fine).
- Per-tournament cadence overrides.
- Distributed lock (only one app instance for now).

### Estimated effort

1–2 days for an MVP that runs all three loops, respects an env flag, and writes a structured
status row per cycle so we can debug from logs without attaching a debugger.

---

## 2. Oracle's Elixir as a secondary stats source

### Problem

Leaguepedia gives us KDA, items, draft, and team objectives — but **no per-game timing data**
(gold@10/15/20, CS@10/15, gold diff, etc.) and `Gamelength` is broken in current Cargo schema (see
postmortem). For richer match-detail UI (early-game / lane-phase analysis) we need a second source.

### Proposed source

[Oracle's Elixir](https://oracleselixir.com/tools/downloads): one CSV per year, ~50 MB,
updated **weekly**. Public domain. Used by the entire LoL analytics community.

### What we'd gain

- `gamelength` (real in-game seconds, not wall-clock).
- `goldat10 / 15 / 20`, `xpat10 / 15`, `csat10 / 15`, `golddiffat10 / 15`, `csdiffat10 / 15`,
  `killsat10 / 15`.
- `damagetakenperminute`, `wpm`, `wcpm`, `vspm`, `earned gpm`, `gold spent` per player.
- `patch` per game.

### What we'd lose vs. Leaguepedia

- Dragon splits (cloud / infernal / mountain / ocean / hextech / chemtech) — OE only has total `dragons`.
- Items per player.
- Real-time updates (OE lags a few days behind, fine for everything except live ongoing matches).

### Architecture

- New service `OracleElixirImportService` in `RiftVeil.Infrastructure/Services/Import/`.
- Daily download of `<year>_LoL_esports_match_data_from_OraclesElixir.csv` (or quarterly snapshot
  if file size grows).
- New table `GamePhaseStats` (per-player and/or per-team @10/@15/@20 snapshots). Foreign key to
  `Game`.
- Mapping `OE.gameid` ↔ `Game.ExternalId` (Leaguepedia GameId): tournament + date + team names is
  almost always 1:1. Manual override table for the 1–2 % that don't match.

### Estimated effort

3–5 days: CSV download + parse + schema + mapping + UI (phase comparison cards on `MatchDetail`).

### Why "later"

Useful but not critical. The current scoreboard and draft are enough to ship v1. Re-evaluate when
the basic site has stable users and we want analytical depth.

---

## 3. In-game event timeline (highlights / "first blood at 03:01", "baron at 21:14")

### Problem

We want a per-game event list with timestamps:

```
03:01  First Blood (Caedrel)
04:37  Team fight (top)
08:31  Cloud Drake (BLG)
14:08  Team fight (bot)
15:17  Ace (DK)
15:57  Baron (T1)
```

### Why it's hard

This data only exists in **Riot Match Timeline** files. Three realistic sources:

| Source | Effort | Risk |
|---|---|---|
| Scrape `lolesports.com` spectator/timeline endpoints | Medium (reverse-engineering, undocumented) | High — Riot breaks these regularly |
| Riot LoL Esports API | n/a | Practically inaccessible without partnership since ~2023 |
| Bayes Esports / GRID | Low (commercial SDKs) | Cost — B2B contracts, not for hobby projects |

Neither **Leaguepedia** nor **Oracle's Elixir** publishes raw events.

### Recommendation

**Skip for now.** Display "Coming soon" in the highlights sidebar. Revisit if/when:

1. We're prepared to scrape lolesports endpoints and accept the maintenance burden, or
2. We get B2B access to Bayes/GRID, or
3. Riot reopens public esports API access (unlikely).

A halfway feature could be **manual event entry per match** in `/admin` — useful for marquee
matches, terrible at scale. Not worth building unless someone is willing to curate.

---

## 4. Smooth gold/XP graph

Same data dependency as #3. Without per-minute Riot timeline data, the only available points are
Oracle's Elixir snapshots at 10/15/20 min — three points, not enough for a "smooth" graph.

**Recommendation:** display **phase comparison cards** instead of a graph (gold/CS/XP @10/15/20
side-by-side per role). More informative than a 3-point line, and possible with #2 alone.

If a real graph becomes a hard requirement, it gets blocked on the same data sources as #3.

---

## 5. Objectives sidebar — element splits (optional follow-up)

**Shipped (2026-05-25):** `GameObjectives` on match detail shows per-team totals for towers,
dragons (aggregate), barons, and void grubs from `GameTeamStats` / `GET /api/games/{id}/details`.

**Still optional later:** dragon splits by element (cloud / infernal / …), herald, inhibitors,
and richer objective tooltips — data exists in Cargo but is not surfaced in the current UI.

---

## 6. Live status: from estimate to reality

### Current state (shipped)

`MatchProjections.ToListItemDto` / `ToDetailsDto` derive `Status = Live` whenever:

```
Status == Scheduled
  AND StartsAtUtc <= now
  AND now <= StartsAtUtc + (BestOf * 75 min)
```

Same heuristic is used in `MatchReadService.GetLiveAsync` so `/api/matches/live` returns the
same set as the LIVE badge in the UI.

This is a **time-window estimate** — there's no actual signal from Leaguepedia or Riot saying
"this match is happening right now". Known artifacts:

- **False positive (long tail):** A BO3 that ends 2-0 in 50 min still shows LIVE for ~3 h after
  the actual finish, until the next manual import flips it to Finished.
- **False positive (short tail):** A BO1 that ends in 18 min shows LIVE for the rest of the 75 min
  window.
- **False negative (rare):** Match starts a few minutes before its scheduled time. We wait until
  `StartsAtUtc` regardless.

Acceptable for now because (a) the live view is rarely used pre-production and (b) the next
manual `/admin` import always corrects it.

### Phase 1 — real polling (covered by #1)

When the auto-import in #1 lands, the matches loop should poll Leaguepedia every 1–5 min for
matches whose `StartsAtUtc` is within the live window, calling `Match.MarkLive` /
`Match.MarkFinished` based on actual wiki state. Once Status in DB is trustworthy, the heuristic
in `MatchProjections` becomes a fallback (kept anyway, costs nothing) and the false-positive tail
shrinks from hours to ~5 minutes.

Concrete implementation sketch:

```csharp
// Smallest viable v0 — narrower than full #1 if we want it sooner.
public class LiveMatchPoller(IServiceProvider services) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiftVeilDbContext>();
            var importer = scope.ServiceProvider.GetRequiredService<LeaguepediaImportService>();

            var now = DateTimeOffset.UtcNow;
            var liveCandidates = await db.Matches
                .Where(m => m.Status != MatchStatus.Finished
                         && m.Status != MatchStatus.Cancelled
                         && m.StartsAtUtc <= now
                         && m.StartsAtUtc.AddMinutes(m.BestOf * 75.0) >= now)
                .ToListAsync(ct);

            foreach (var match in liveCandidates)
                await importer.RefreshMatchStatusAsync(match);
        }
    }
}
```

Cost estimate: 4-6 simultaneous matches × 1 Cargo query/min × 4 h = ~1500 requests/day during a
busy LCK+LEC weekend. Bot limit is ~200 req/min, so well within budget.

### Phase 2 — broadcast-aware "live"

A match is *broadcast* live earlier than it's *played*: pre-show, casters, analyst desk, draft
discussion, sometimes interviews. For viewers who want to catch the whole show, "Upcoming" is
wrong from the moment the stream goes live (lolesports.com shows a "Broadcast starting in
21:46" countdown screen — that screen is itself a live video, just before the match starts).

The right answer is to ask the actual broadcasters whether they're live, not to guess from the
clock. Twitch and YouTube both expose this via their public APIs.

#### Recommended path: Twitch Helix API

Twitch is the primary stream for every major LoL league (LCK, LEC, LCS, LPL, LTA), so a single
provider integration covers ~95 % of the value.

- Endpoint: `GET https://api.twitch.tv/helix/streams?user_login=<channel>`
- Returns one stream object when the channel is live, empty array when it's not.
- Includes `started_at`, `viewer_count`, `title`, `game_name` — useful both for triggering Live
  and for filtering out off-day SoloQ streams.
- Pre-show / countdown screens **count as live** — exactly what we want.

Auth: register a free app on [dev.twitch.tv/console](https://dev.twitch.tv/console), get
Client ID + Secret. Use OAuth client_credentials flow for an App Access Token (~60-day lifetime,
auto-refresh on 401). No user interaction needed.

Rate limit: 800 requests/minute for app tokens. We need ~1 request/minute/league during a live
window (4–6 active leagues max in a typical day) → trivially within budget.

Known channel mapping:

| League | Twitch login |
|---|---|
| LCK | `lck` |
| LEC | `lec` |
| LCS | `lcs` |
| LPL | `lpl` |
| LTA North / South | `lta_north` / `lta_south` |
| Worlds / MSI | `riotgames` |

#### Why not YouTube as primary

- YouTube Data API v3 free tier is **10 000 quota units/day** total.
- `search.list?eventType=live` costs **100 units per call** → max 100 channel-checks/day.
  Inadequate for a per-minute polling loop.
- `videos.list?part=liveStreamingDetails` is 1 unit per call but requires knowing the live
  video ID up-front, which means scraping the channel uploads first → another quota cost.
- Worth adding as a fallback only for leagues that don't stream on Twitch (LCK simulcasts on
  YouTube; not critical to detect since Twitch already tells us).

#### Architecture sketch

```csharp
public class BroadcastWatcher(
    IServiceProvider services,
    TwitchApiClient twitch,
    ILogger<BroadcastWatcher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        while (await timer.WaitForNextTickAsync(ct))
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RiftVeilDbContext>();

            // Only check leagues that have a match within a sensible window — avoids
            // false positives from off-day SoloQ streams and developer talks.
            var now = DateTimeOffset.UtcNow;
            var leaguesWithUpcomingMatches = await db.Leagues
                .Where(league => league.TwitchChannel != null
                    && db.Matches.Any(match =>
                        match.Tournament.LeagueId == league.Id
                        && match.Status != MatchStatus.Finished
                        && match.StartsAtUtc.AddMinutes(-90) <= now
                        && match.StartsAtUtc.AddHours(6) >= now))
                .ToListAsync(ct);

            foreach (var league in leaguesWithUpcomingMatches)
            {
                var stream = await twitch.GetStreamAsync(league.TwitchChannel!, ct);
                if (stream is null || stream.GameName != "League of Legends")
                    continue;

                // Mark the closest upcoming match in this league as Live.
                var match = await db.Matches
                    .Where(m => m.Tournament.LeagueId == league.Id
                             && m.Status == MatchStatus.Scheduled
                             && m.StartsAtUtc.AddHours(6) >= now)
                    .OrderBy(m => m.StartsAtUtc)
                    .FirstOrDefaultAsync(ct);

                if (match is not null)
                {
                    match.MarkLive(stream.StartedAt);
                    await db.SaveChangesAsync(ct);
                }
            }
        }
    }
}
```

#### What we'd need

1. Add nullable `TwitchChannel` (string) to `League`. Backfill manually once.
2. `TwitchApiClient` with OAuth handling and a minimal `GetStreamAsync(channel)` method.
3. `BroadcastWatcher` background service.
4. Configure Client ID / Secret via user-secrets (locally) and env vars (production).

#### Edge cases

- **Channel live for unrelated content** (caster SoloQ, dev talk, off-day stream) → mitigate by
  windowing on `StartsAtUtc - 90 min` and filtering on `game_name == "League of Legends"`.
- **Re-broadcasts / re-runs** → check `started_at` is recent (within last few hours).
- **Multi-stage events** (Worlds days with 4+ matches): the same pre-show announces all of them.
  "First upcoming" gets flagged Live as soon as the stream starts. Acceptable approximation.
- **Channel takes a break between matches** (small 5–15 min gap) → `is_live` flips off and on;
  add a debounce so we don't bounce Status repeatedly. E.g. only flip back to Scheduled if
  channel has been off for >30 min and no new winner has been recorded.

#### Effort

MVP (Twitch only, hard-coded channel mapping, no debounce): **~4–6 h**.
Production-ready (per-league config table, OAuth refresh, structured logging, debounce, YouTube
fallback): **~2 days**.

#### Recommendation

Build **Phase 1** first to get the Status field trustworthy from real wiki data. Then build
this on top — `MarkLive` becomes a triggerable event from multiple sources (broadcast watcher
*or* match poller, whichever fires first). The heuristic in `MatchProjections` stays as a
final fallback that costs nothing.

---

## 7. Real logo URLs (league, team, tournament)

### Problem

Logos are mostly managed manually in `frontend/public/logos/...`. It works, but it is repetitive
and gets harder as more leagues/teams/tournaments are added.

### Goal

Use real `logoUrl` data from APIs/importers as the primary source, with local files as fallback.
The app should still render safely even when a logo is missing.

### Plan

- Keep fallback behavior in the UI (`logoUrl` -> local file -> placeholder).
- Start with leagues and teams first.
- Add tournament logos as a second phase if needed.
- Add a lightweight "logo sync" flow so we do not depend on manual downloading.

### Scope and phases

1. **Phase 1 (safe rollout):** support league/team `logoUrl` in API + frontend fallback.
2. **Phase 2:** improve importer coverage so more leagues/teams get URLs automatically.
3. **Phase 3 (optional):** add `Tournament.LogoUrl` and show tournament logos in UI.

### Why this is later

Current manual flow is usable and low risk. This project improves maintainability and scaling,
but is not blocking core match/tournament functionality.

---

## 8. Bracket-aware spoiler protection

### Problem

Spoiler mode today hides **scores** for finished matches, but upcoming bracket matchups can still
reveal **who plays whom** before earlier rounds are resolved. Example: a semifinal card may show
both team names even if the user has not watched (or revealed) the quarterfinal that decides one
of those slots.

That breaks the intended spoiler experience for playoffs — users should not learn a future
opponent from a later-round fixture.

### Goal

Upcoming matches whose participants depend on unresolved earlier games should stay hidden (or show
neutral placeholders) until the prerequisite match(es) are no longer spoiler-sensitive for that
user.

### Rough approach

- Model bracket dependencies per tournament stage (which match feeds which slot).
- Treat a matchup as spoiler-safe only when all upstream matches are either:
  - not yet played, **and** we don't know participants yet, or
  - already revealed by the user (per-match reveal), or
  - finished with spoilers globally enabled / explicitly shown.
- UI: show `TBD` / generic placeholders for undetermined or locked slots; reuse the TBD logo work
  in `todo.md`.
- Extend `useSpoilerPrefs` (or a sibling hook) with bracket context — not just
  `revealedMatchIds`, but "this slot is unlocked because upstream match X was revealed".

### Open questions

- Do we get bracket linkage from Leaguepedia import, or infer from round names + schedule?
- Should revealing a quarterfinal auto-unlock only that semifinal slot, or the whole round?
- Same rules on home, league hub, and match detail cross-links?

### Why later

Needs bracket metadata and clearer product rules. Current per-match score hiding is enough for
regular season; this matters most for playoffs and double-elim formats.
