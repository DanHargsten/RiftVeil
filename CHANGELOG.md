# Changelog

All notable changes to **RiftVeil** are recorded here, **newest first**. Sections are dated (no version numbers). Where it helps, entries are grouped like [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) (Added / Changed / Fixed / Removed).

## 2026-05-06

### Added

- **`LeaguepediaClientOptions`** and **`Leaguepedia`** configuration section: Cargo pacing (**`PostSuccessDelayMilliseconds`**), delays between game-detail phases and between ongoing-tournament imports, **`CargoPageSize`** for pagination, rate-limit and transient API backoff (**`RateLimitMaxAttempts`**, extended cooldown, exponential caps, **`TransientApiErrorBackoff*`**).
- API project: **`UserSecretsId`** for local configuration (**`dotnet user-secrets`**).
- **`LolesportsClientOptions`**: **`MaxAttempts`** and **`RetryDelayMilliseconds`** for GraphQL retries.

### Changed

- **`LeaguepediaClient`**: binds **`IOptions<LeaguepediaClientOptions>`**; process-wide **`SemaphoreSlim`** so Cargo requests run sequentially; **`offset`** for paged queries; stream-based JSON parse; retries with separate backoff paths for **ratelimited** vs **`internal_api_error_*`**; post-success delay from config.
- **`GameDetailImportService`**: **`FetchAllCargoPagesAsync`** over **ScoreboardPlayers**, **ScoreboardTeams**, and **PicksAndBansS7** (stable **`order by`**, configurable page size); phase delays from options; draft rows use **`team`** from pick/ban columns directly for **`teamNumber`** (removes incorrect **`ResolveTeamNumberFromPickBan`** mapping).
- **`ImportController`**: injects **`LeaguepediaClientOptions`** for **`DelayBetweenOngoingTournamentsMilliseconds`**; ongoing matches action renamed **`ImportOngoingMatchesAsync`** with **`ProducesResponseType`** and shared league lookup.
- **`Program`**: registers **`LeaguepediaClientOptions`**; Leaguepedia **`HttpClient`** uses identifiable **`User-Agent`**, **gzip** **`Accept-Encoding`**, and **`DecompressionMethods.All`**; **`UseCors`** runs outside the development-only block so CORS applies in all environments.
- **`LolesportsClient`**: **`ILogger<LolesportsClient>`** instead of noisy **`Console`** output; retry loop driven by options.
- **`appsettings` / Development**: **`Leaguepedia`** + expanded LoLesports keys; **commit no longer stores a real LoLesports API key** in Development — use user secrets or environment variables.

### Changed (frontend)

- **`GameDraft`**: **`team1Side`** places blue side left (swap columns when team 1 is red); optional pick-order badge (**`draft__champ-seq`**); ban icons updated.
- **`GameScoreboard`** / **`GamePanel`**: **`team1Side`** reorders teams so blue side renders in the left column.
- **`game-details.css`**: ban styling (**`draft__champ-seq`**, overlay); scoreboard team header tints (**`color-mix`**).

## 2026-04-21

### Added

- Frontend: **`GameDraft`** and **`GameScoreboard`** on the match detail page, driven by **`GET /api/games/{gameId}/details`** via **`gamesApi.getDetails`** and client DTOs aligned with **`GameDetailsDto`** (**`PlayerStatsDto`**, **`TeamStatsDto`**, **`DraftEntryDto`**).
- Frontend: **`useItemLookup`** hook — Data Dragon **`item.json`** (cached with React Query) for item name → icon URL resolution on the scoreboard.
- Frontend: **`MatchHero`**, **`GameTabs`**, and **`GamePanel`** — match detail route composes these instead of a single large component; **`GamePanel`** owns VOD, loading/error, draft, and scoreboard for the selected game.
- Styles: **`styles/pages/game-details.css`** — draft/scoreboard layout, loading and error states, section chrome, and outer match-detail grid (moved from **`styles/game-details.css`** for consistency with other page stylesheets).
- Import API: **`POST /api/import/game-details/ongoing`** — imports game details for all **ongoing** tournaments (sequential processing with a delay between tournaments).
- Admin: import step **Game Details (all ongoing)** calling the new endpoint.

### Changed

- **`MatchDetail`**: per-game query for details when a played game tab is selected; draft and scoreboard replace placeholders; loading and error UI; tab list / tab panel semantics and **`sr-only`** headings for accessibility.
- **`MatchDetail`**: outer **`match-detail__outer`** layout with left/right sidebars (objectives and highlights placeholders); root **`.page`** uses **`display: contents`** so the grid participates in the parent flex layout (**`layout/app.css`**).
- **`match-detail.css`**: page chrome adjustments (e.g. padding); placeholder regions sized for upcoming content.
- **`GameScoreboard`**: **`TeamLogo`** in team headers; player names strip parenthetical suffixes for display.

## 2026-04-18

### Added

- Read API: **`GET /api/games/{gameId}/details`** returning **`GameDetailsDto`** (core game fields, **`Team1Players`** / **`Team2Players`**, **`Team1Stats`** / **`Team2Stats`**, ordered **`Draft`**).
- Application DTOs under **`GameDetailsDto`**, **`PlayerStatsDto`**, **`TeamStatsDto`**, **`DraftEntryDto`**; **`IGameReadService`** and infrastructure **`GameReadService`** (single projected query, lane ordering for display).
- API: **`GamesController`** and DI registration for **`IGameReadService`**.

### Changed

- **Leaguepedia draft import** (`PicksAndBansS7`): Cargo field list and pick resolution use **`Team{n}Pick{k}`** instead of **`Team{n}Role{k}`** to match current Leaguepedia column names.

## 2026-04-17

### Changed

- `GamePlayerStats`: property `Role` renamed to **`IngameRole`** to match Leaguepedia Cargo field naming; EF migration `RenameRoleToIngameRole` and snapshot update.

## 2026-04-16

### Added

- Documentation: root **`README.md`** — current features (Home, leagues, match detail, admin), Leaguepedia/Lolesports data flow, tech table, repo layout, local dev steps, link to this changelog.
- Import API: **`POST /api/import/backfill-game-ids/{leagueShortName}`** returns JSON `{ gamesUpdated, tournamentsSkipped }`; backfills **`Game.ExternalId`** from **`MatchScheduleGame`** (one Cargo query per tournament).
- Domain: **`Game.SetExternalId`** with validation for Leaguepedia `GameId` backfill.

### Changed

- **Leaguepedia client**: shorter rate-limit backoff after `ratelimited`; shorter delay between tournaments during backfill.
- **Leaguepedia import service**: domain-oriented lambda parameter names in backfill-related LINQ.

## 2026-04-15

### Added

- Backend: `GamePlayerStats`, `GameTeamStats`, and `GameDraftEntry` entities linked to `Game`, with EF Core configuration, check constraints, and migrations (`AddGameDetailStats`, `RefineGameDetailStats`).
- Frontend: league hub at `/leagues/:shortName` (tournament picker, matches by round, spoiler prefs).
- Frontend: `/admin` for import jobs (tournaments, matches, VODs); import controls removed from the main navbar.

### Changed

- Match detail page: hero header (league, stage, scoreline), tabs for played games only, VOD button, and placeholder sections for draft, scoreboard, gold, and objectives.
- Navbar: league menu driven by the leagues API with `LeagueLogo`; navigation to league routes.
- App routing: `/leagues` redirects to home; tournament list route removed in favour of league-centric URLs.
- Styles: match detail, league page, admin, navbar; baseline reset and `index.css` (including shared button styles).

## 2026-04-06 – 2026-04-13

### Frontend

- `useSpoilerPrefs` hook; Home page refactored around spoiler toggles and list sections.
- Team and league images in `public/`; `TeamLogo` and `LeagueLogo` consolidated into one module; expanded icon set.
- Global and component CSS: layout/spacing, match list responsiveness, navbar (import menu era), new `badge` and `sidebar` stylesheets, colour variables, and reset rules; Home styles trimmed as layout moved to components.
- MatchCard redesign (structure, status styling, league/tournament header); navigation and match list behaviour updated.
- Frontend/editor tooling configuration checked in alongside assets.

### Backend

- Ongoing-matches import API and import pipeline timing tweaks.
- Matches/games/VOD handling; `SetWinningTeam` and projections; controller and DTO clean-up; tests and formatting.

## 2026-03

- `GameVod` entity, migration, and Lolesports-based VOD enrichment.

## 2026-02

### Frontend

- Home page with live, upcoming, and recent matches.
- Match detail page (first version) and reorganised CSS structure.
- Games surfaced on match cards; table-style views for leagues and tournaments.
- Tournament sidebar and broader layout tweaks.
- “Load more matches” control (WIP) and shared CSS variables.
- Iterations on MatchList/MatchCard, icons, and hover/focus styling.
- Navbar with client-side routing (early shell).

### Backend

- Leaguepedia tournament and match import, batching/caching, winner mapping fixes.
- `Team` entity and migrations; `Round` on `Match`.
- C# control-flow style rule (mandatory braces); XML docs and whitespace cleanup.

## 2026-01

### Frontend

- Vite app scaffold.
- Frontend wired to the matches API (DTO alignment, client fetch layer).

### Backend

- Initial API, domain, EF Core, and smoke tests.
