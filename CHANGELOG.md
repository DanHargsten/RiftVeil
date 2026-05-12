# Changelog

Notable **user-facing and behavioural** changes to **RiftVeil**, **newest first**. Grouped like [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) where it helps.

For class-level detail, heuristics, and file references, see [`docs/technical-log.md`](docs/technical-log.md). For Leaguepedia / LoLesports import pitfalls and SQL cleanups, see [`docs/data-import-notes.md`](docs/data-import-notes.md). Roadmap and future ideas stay in [`docs/future-projects.md`](docs/future-projects.md).

Early months (2026-01 through ~2026-04-13) are summarised at a high level; the project log was backfilled from memory and commits before day-by-day notes started.

## 2026-05-12

### Added

- **`leagueRegion`** on **`MatchListItemDto`** / list projections and **`MatchListItem`** in **`frontend/src/lib/api.ts`**, exposed for client-side search and display.
- Optional **site footer** with Riot notice and **`VITE_CONTACT_EMAIL`** mailto: **`SiteFooter.tsx`**, **`site-footer.css`**, **`frontend/.env.example`**, **`vite-env.d.ts`**.

### Changed

- Frontend **routing shell**: **`AppShell`** in **`App.tsx`** wraps **`Outlet`** in a single **`main.app-main`** (header + footer outside) so the document keeps **one** `<main>` landmark.
- **Match list**: grouped by local date with optional empty **Today** row when filtering, **search** + **tournament** controls with improved labels/ids for assistive tech; sticky toolbar styling updates (**`match-list.css`**).
- **Styles**: CSS entry via **`styles/index.css`** only — removed obsolete **`layout.css`** / **`typography.css`**; **`reset.css`**, **`app.css`**, **`home.css`**, and several page/component sheets adjusted; **section comment style** aligned (banner + short labels) across navbar, league, admin, game-details, badge, table, variables, etc.
- **`ImportController`**: **`[ProducesResponseType]`** on selected backfill / ongoing import endpoints for clearer OpenAPI output.

### Removed

- Dead **navbar dev-import (hamburger) CSS** (`.import-menu*`) — imports are **`/admin`** only; **`Navbar.tsx`** already had no markup for it.

### Fixed

- **`MatchReadService.GetLiveAsync`**: comment wording (neutral voice) next to live-status heuristic.

## 2026-05-11

### Added

- Match detail page: **Objectives** sidebar (towers, total dragons, barons, void grubs) from existing game-details **`TeamStatsDto`**, two-column team comparison (**`GameObjectives.tsx`**, match detail layout/CSS).

### Fixed

- Fixed draft import mapping for Leaguepedia **`PicksAndBansS7`**: wiki blue/red columns are mapped to local **`Team1`** / **`Team2`** using game side data (same model as other game-detail stats).
- Games missing side data no longer get incorrect draft rows; those games are skipped for draft import.

### Notes

- Existing **`GameDraftEntries`** for games where **`Team1Side = Red`** may be wrong; delete and re-import game details if needed — see **Migration** in [`docs/data-import-notes.md`](docs/data-import-notes.md).

## 2026-05-08

### Added

- Estimated **live** match status in the API and UI (**`MatchStatus.Live`** within a configurable window after **`StartsAtUtc`**, default **`BestOf × 75`** minutes) so lists match real schedules without a background poller yet.
- Match cards: clearer **Live** styling (accent stripe, reduced-motion-friendly pulse on the badge), **Watch live** link to LoLesports, and round / best-of shown in the footer.

### Changed

- Game detail import for a tournament skips **unplayed** games (no winner), cutting Cargo work on phantom bracket games.
- Team name matching tolerates Leaguepedia **disambiguation suffixes** so side mapping and stats import do not silently skip teams.
- **VOD** league enrichment skips LoLesports seasons that already have full VOD coverage; removed unnecessary hard-coded delays (retries still paced by client options).
- **`/api/matches/live`** aligned with the same live heuristic the frontend **LIVE** badge uses; all match reads use one **`UtcNow`** per request for consistent derived status.
- Match card layout: only **Live** keeps a strong accent stripe; upcoming/finished stripes removed as noise; centre label **vs** for all states including live.

### Removed

- Config and code for **delay between ongoing tournament** detail imports (Cargo pacing handles spacing); removed unused **live-only “VS”** label CSS/JSX.

## 2026-05-07

### Added

- Safer Leaguepedia Cargo usage: query outcome vs empty, optional **bot login**, capped transient retries, configurable delay between **match-import** tournaments.
- **Tiered `ScoreboardTeams`** Cargo queries with automatic fallback to narrower field sets; single-game **`POST /api/import/game-details/game/{id}`**; dev **Import details** on match UI.
- Extended docs in **`docs/future-projects.md`** (background import, Oracle's Elixir, timeline/highlights, gold graph, objectives sidebar).

### Changed

- Better logging for Cargo **rate limits** and **transient** errors; **`LeaguepediaImportService`** uses options instead of fixed sleeps between phases.
- **Team stats** import survives Cargo failures without silently importing zero rows; game length from Cargo omitted when unstable, with fallback to stored **`Game.Duration`**.

## 2026-05-06

### Added

- Configurable Leaguepedia **pacing**, pagination, and backoff; API **user secrets** id; LoLesports GraphQL **retry** options.

### Changed

- Leaguepedia **HttpClient** hardening (user agent, compression, CORS in all environments); LoLesports client uses **structured logging** instead of console noise; **no real API keys** committed in Development settings.

### Changed (frontend)

- Draft and scoreboard **respect blue/red side** for column order; draft pick-order hint and ban presentation tweaks.

## 2026-04-21

### Added

- Match detail: **draft** and **team scoreboard** with Data Dragon **item** icons; split **GamePanel** / tabs / hero layout; stylesheet under **`styles/pages/`**.
- **`POST /api/import/game-details/ongoing`** and admin control for **all ongoing** tournaments.

### Changed

- Match detail UX: per-game fetch, accessibility for tabs, sidebars for future objectives/highlights; scoreboard shows **team logos** and cleaner player names.

## 2026-04-18

### Added

- **`GET /api/games/{gameId}/details`** and **`GameDetailsDto`** pipeline (players, teams, draft).

### Changed

- Draft import updated for current **`PicksAndBansS7`** **Pick** column names.

## 2026-04-17

### Changed

- **`GamePlayerStats.Role`** renamed to **`IngameRole`** (EF migration).

## 2026-04-16

### Added

- Root **README** (features, data flow, local dev); **`POST /api/import/backfill-game-ids/{league}`**; **`Game.SetExternalId`**.

### Changed

- Leaguepedia client **shorter backoff** after rate limits and during backfill.

## 2026-04-15

### Added

- **Game detail stats** entities and migrations; **league hub** and **`/admin`** import UI.

### Changed

- Match detail **hero + tabs + VOD**; navbar **leagues**; routing centres on **`/leagues/:shortName`**; broad style pass.

## 2026-04-06 – 2026-04-13

### Frontend

- Spoiler prefs, logos, MatchCard/list polish, layout and **badge** styling, tooling config.

### Backend

- Ongoing import API, match/game/VOD behaviour and tests.

## 2026-03

- **Game VOD** storage and LoLesports-based enrichment.

## 2026-02

### Frontend

- Home (**live / upcoming / recent**), first match detail, games on cards, league views, layout iterations, routing shell.

### Backend

- Leaguepedia import improvements; **`Team`** and **`Match.Round`**.

## 2026-01

### Frontend

- Vite scaffold and matches API integration.

### Backend

- Initial API, domain, EF Core, smoke tests.
