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
| **Matches** | every 30 min, or every 5 min during a known match window | `ImportOngoingMatchesAsync` per league. Updates `Status` from `Scheduled` → `Live` → `Finished`. |
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

## 5. Objectives sidebar (already has data, needs UI)

The match page has an empty "Objectives" sidebar. Per-team objective totals
(barons, dragons split by element, void grubs, herald, towers, inhibitors) are already imported
into `GameTeamStats` — just need a presentational component reading the existing
`GET /api/games/{id}/details` payload. Small UI ticket, not an architecture project.
