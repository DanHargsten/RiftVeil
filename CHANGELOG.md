# Changelog

Notable **user-facing and behavioural** changes to **RiftVeil**, **newest first**. Grouped like [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) where it helps.

For class-level detail, heuristics, and file references, see [`docs/technical-log.md`](docs/technical-log.md). For Leaguepedia / LoLesports import pitfalls and SQL cleanups, see [`docs/data-import-notes.md`](docs/data-import-notes.md). Roadmap and future ideas stay in [`docs/future-projects.md`](docs/future-projects.md).

Early months (2026-01 through ~2026-04-13) are summarised at a high level; the project log was backfilled from memory and commits before day-by-day notes started.

## 2026-05-22

### Added

- **Match detail — damage breakdown** — lane-by-lane damage bars beside global objectives, with Game vs Team scaling and blue/red side layout aligned to the scoreboard.
- **Match detail — hero footer** — game tabs, VOD link, and dev import controls sit under the match hero instead of inside the game panel.
- **League logos** — `cblol.png` and `lcp.png` under `frontend/public/logos/leagues/`.
- **Team logos** — refreshed wordmarks and new regional team assets (including square variants for scoreboard/draft headers).

### Changed

- **Match detail layout** — stacked game panel (draft + scoreboard, then objectives + damage); large team watermarks in the hero; VOD moved to hero footer.
- **Game panel refactor** — display order follows blue/red side; logic extracted into colocated utils (`laneMatchupUtils`, `damageBarUtils`, `draftUtils`, `scoreboardUtils`, `matchDisplayUtils`).
- **Desktop-first CSS** — viewport `@media` rules removed from home, league, match list, sidebar, match detail, and game details for now.
- **League hub** — when the selected tournament is ongoing, only the active round expands by default.
- **React Query Devtools** — shown only when `VITE_SHOW_QUERY_DEVTOOLS=true`.

### Notes

- Responsive/mobile layout is tracked in `docs/todo.md`. Bracket-aware spoiler protection is scoped in `docs/future-projects.md`.

## 2026-05-20

### Added

- **CBLOL and LCP** — new leagues in DB seed, Admin import/backfill selectors, Leaguepedia import mapping, and LoLesports VOD slug map. Tournament import falls back to `OverviewPage LIKE "{SHORT}/%"` when Cargo `League` returns no rows (same pattern as LPL).
- **LCK seed** — `LCK` is now ensured on startup like the other major leagues (was selectable in Admin but missing from the default seed).
- **Team logos from Leaguepedia** — `Team.IconLogoUrl` (square/isotype) plus richer `LogoUrl` from Cargo `Teams.Image`; match list/detail APIs expose `team*LogoUrl` and `team*IconLogoUrl`.
- **Admin — Teams tab** — list/search teams, edit metadata, per-row **Sync LP**, orphan delete, and problem filters (missing icon/logo/short).
- **Admin — scoped imports** — default **last 7 days** for matches, VODs, and game details; **Backfill** tab with game IDs, game sides, and team metadata jobs.
- **Import API** — `POST /api/import/matches/{league}/recent`, `POST /api/import/vods/{league}/recent`, `POST /api/import/backfill-teams`, `GET/PATCH /api/teams`, sync and delete endpoints.

### Changed

- **Team logo display** — UI resolves `local {short}-square.png` → `{short}.png` → remote icon URL → placeholder, with on-error fallback chain.
- **Admin layout** — `/admin` split into Import, Backfill, and Teams tabs (keyboard-friendly tablist and live regions for job status).
- **Leaguepedia team preload** — wider Cargo region list and `OverviewPage` on import/backfill; square icon URLs verified via authenticated Fandom `Special:FilePath` when possible.

### Notes

- Apply EF migration **`AddTeamIconLogoUrl`** before using team backfill or the Teams admin tab.

## 2026-05-19

### Added

- **Match detail — lane scoreboard** — player stats are shown as lane-by-lane matchups (top through support) in one table, with role icons and champion/item art loaded from the current Data Dragon patch.
- **Match detail — draft summary** — team logos, KDA and gold totals with icons, and a WIN badge on the winning side above the draft picks.

### Changed

- **Typography** — site-wide font stack updated to **Space Grotesk** with **Orbitron** on the navbar brand.
- **Match detail layout** — single centred column; game tabs sit under the match hero; **Global objectives** and **Highlights** live inside the active game panel (side columns removed).
- **Match cards** — winning team is emphasised and the loser is subdued when scores are visible; clearer upcoming start time and an explicit **Watch VODs** label when game links exist.
- **Home sidebar** — tournament sidebar owns its right border and background so the divider stays aligned while scrolling.
- **Item and champion icons** — match views use the latest **Data Dragon** patch from Riot’s CDN instead of a fixed hard-coded version.

### Notes

- The lane scoreboard hides the damage column on match detail for now; damage remains available in the component for other views.

## 2026-05-13

### Added

- **Admin import controls** — clearer import scopes (ongoing vs all), tournament-targeted Game Details import mode, and improved guidance for heavier jobs.
- **LPL support** — `LPL` available in admin league selection and in import mappings for tournaments/matches/VODs.
- **Docs planning split** — new lightweight `docs/todo.md` for short-term fixes, while larger scoped items remain in `docs/future-projects.md`.

### Changed

- **League view dates** — round date ranges now include year context so ranges are unambiguous.
- **DB seed behavior** — default leagues are now ensured incrementally (missing leagues are added without requiring an empty table).

### Notes

- LPL VOD enrichment currently reaches lolesports tournaments but still often returns zero completed events / enrichments in this period; this appears to be upstream data-shape/timing rather than a simple missing-channel issue.

## 2026-05-12

### Added

- **Match list** — search across teams, tournament name, and **region**; dates grouped by calendar day with a clearer **Today** band when filtering.
- **Footer** — Riot “Legal Jibber Jabber” notice on every page; optional **contact** link when you set the contact email in the frontend environment.

### Changed

- **Layout** — one primary **main** content area for the whole app (better for screen readers and skip links); header and footer stay outside the scrolling content column.
- **Match list toolbar** — easier to use with keyboard and assistive tech (labels tied to search and filters).
- **Imports** — all import actions stay under **`/admin`** only; leftover navbar styles for an old import menu are gone.

### Removed

- Legacy global **`layout.css`** / **`typography.css`** entry files from the stylesheet bundle (imports are centralised through **`index.css`**).

### Notes

- API list payloads now include **league region** so the new search can work without extra requests; OpenAPI shows clearer status codes on a few maintenance endpoints. Details: **`docs/technical-log.md`**.

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
