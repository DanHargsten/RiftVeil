# Changelog

All notable changes to **RiftVeil** are recorded here, **newest first**. Sections are dated (no version numbers). Where it helps, entries are grouped like [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) (Added / Changed / Fixed / Removed).

> The earliest entries (2026-01 through ~2026-04-13) are intentionally written at a high, summary level — this changelog was started a few weeks into the project and the early period is reconstructed from memory and commit history rather than tracked day-by-day. From mid-April onwards entries are detailed (class names, methods, files) because they were written while the changes were fresh.

## 2026-05-08

### Added

- **`MatchProjections.LiveWindowMinutesPerGame`** (default **`75`**): heuristic constant used by both **`ToListItemDto(now)`** / **`ToDetailsDto(now)`** and **`MatchReadService.GetLiveAsync`** to derive **`MatchStatus.Live`** for any **`Scheduled`** match where **`StartsAtUtc <= now <= StartsAtUtc + BestOf * 75 min`**. Needed because no auto-import currently calls **`Match.MarkLive`**, so the DB column alone never reflects live state — see **`docs/future-projects.md`** sections 1 + 6.
- Frontend live affordances on **`MatchCard`**: red **`::before`** accent stripe (Live only), pulsing dot in the LIVE badge (**`.badge__pulse`**, respects **`prefers-reduced-motion`**), **Watch live** link to **`lolesports.com/live/<slug>/<slug>`** (helper **`buildLolesportsLiveUrl`**), and a grouped **`Round · Bo3`** block in the footer (**`.match-card__match-meta`**).
- Documentation: **`docs/future-projects.md`** section 6 (live status: from estimate to reality) — Phase 1 real-polling sketch (**`LiveMatchPoller`** **`BackgroundService`**) and Phase 2 broadcast-aware live via **Twitch Helix API** (channel mapping table for LCK / LEC / LCS / LPL / LTA / Worlds, OAuth client_credentials flow, **`BroadcastWatcher`** sketch with windowing on **`StartsAtUtc - 90 min`** and **`game_name == "League of Legends"`** filter, edge cases, effort estimate).

### Changed

- **`GameDetailImportService.ImportGameDetailsForTournamentAsync`**: filter now requires **`Game.WinningTeam != null`** so unplayed phantom games (BO3 game 3 when the series ended 2-0, future scheduled matches) no longer cost a Cargo round-trip per game. Eliminates the bulk of the previous "details took long time" wall-clock cost on incremental imports.
- **`GameDetailImportService.NameMatches`**: tolerates MediaWiki disambiguation suffixes — e.g. **`"LYON (2024 American Team)"`** from Cargo now matches DB **`"LYON"`**. Before, side-mapping silently failed for such teams and player / team stats were skipped without a clear log line.
- **`LolesportsVodEnricher.EnrichLeagueAsync`**: pre-filters lolesports tournaments to those whose date range (±**`14`** day buffer) overlaps a DB tournament that still has games missing a VOD; skips the **`getCompletedEvents`** call (~2.5 s each) for already-fully-covered seasons. Hard-coded **`Task.Delay(500)`** between event details and **`Task.Delay(2000)`** between tournaments removed — **`LolesportsClientOptions.RetryDelayMilliseconds`** already paces requests on transient errors.
- **`MatchReadService`**: all read methods (**`GetAllAsync`**, **`GetUpcomingAsync`**, **`GetRecentAsync`**, **`GetLiveAsync`**, **`GetByIdAsync`**) capture **`DateTimeOffset.UtcNow`** once and pass it to the projections so the derived-Live decision is consistent within a single response. **`GetLiveAsync`** WHERE clause mirrors the projection heuristic so **`/api/matches/live`** returns the same set as the LIVE badge.
- **`MatchCard`** styling overhaul: dropped the colored left-border on Upcoming (yellow) and Finished (gray) — they carried no signal when 90 % of cards in a list shared the stripe — leaving Live as the only state with a colored accent. Live's accent re-implemented as a **`position: absolute ::before`** instead of **`border-left: 3px`** so it sits on top of the footer's **`surface-2`** background and the inner card layout is byte-identical regardless of status. Center-of-card text now reads **`vs`** for Live too (the corner badge, accent stripe, and Watch live CTA already communicate state). **Watch live** button uses smaller padding (**`0.22rem 0.55rem`**) and font (**`0.7rem`**) to match other small footer controls.

### Removed

- **`LeaguepediaClientOptions.DelayBetweenOngoingTournamentsMilliseconds`** and the matching **`appsettings.json`** / **`appsettings.Development.json`** keys: the only consumer (**`ImportController.ImportOngoingGameDetailsAsync`**) no longer needs the per-tournament spacer now that **`LeaguepediaClient.PostSuccessDelayMilliseconds`** paces individual Cargo requests.
- **`.match-card__vs-live`** CSS class and matching JSX branch: middle of the card now shows **`vs`** for Live too, so the dedicated live label was redundant.

## 2026-05-07

### Added

- **`LeaguepediaClient.QueryWithOutcomeAsync`**: returns **`(bool Succeeded, List<JsonElement> Rows)`** so callers can distinguish a Cargo failure-after-retries from a legitimately empty result; **`QueryAsync`** stays as a thin wrapper.
- **`LeaguepediaClient`** bot login: shared **`CookieContainer`** (registered as singleton in **`Program.cs`**), **`EnsureLoggedInAsync`** (one-time per process), **`FetchLoginTokenAsync`** + **`PostLoginAsync`** using **`BotUsername`** / **`BotPassword`** from user secrets — anonymous fallback when credentials are missing.
- **`LeaguepediaClientOptions.MaxTransientRetriesPerQuery`** (default **`3`**): caps **`internal_api_error_*`** retries per query so a known server-side bug stops costing wall-clock time.
- **`LeaguepediaClientOptions.DelayBetweenMatchImportTournamentsMilliseconds`** (default **`1000`**): replaces hard-coded **`Task.Delay(3000)`** in **`LeaguepediaImportService`**.
- **`GameDetailImportService.ScoreboardTeamsCargoFieldTiers`**: ordered list of Cargo field strings (richest → smallest) tried in turn; first tier whose **`QueryWithOutcomeAsync`** call succeeds wins. Logs which tier was used.
- **`GameDetailImportService.FetchCargoForOverviewGameIdsWithOutcomeAsync`** + **`FetchAllCargoPagesWithOutcomeAsync`**: paged variants that propagate the success flag for tiered fallbacks.
- **`GameDetailImportService.ImportGameDetailsForGameIdAsync`**: import a single game by local id (with sides preflight backfill).
- **`ImportController`** `POST /api/import/game-details/game/{gameId:int}`: dev/admin endpoint for per-game detail import.
- Frontend dev-only **Import details** button in **`GamePanel`** / **`MatchDetail`** (visible under **`import.meta.env.DEV`**); shared **`useSpoilerPrefs`** wired through **`Matches`** → **`MatchList`** with **`tournamentId`** prop.
- Documentation: **`docs/future-projects.md`** — design notes for background auto-import, Oracle's Elixir as a secondary stats source, in-game event timeline (highlights), gold/XP graph, and the empty objectives sidebar.

### Changed

- **`LeaguepediaClient`**: response body buffered and **`JsonDocument.Parse(body)`** so transient error bodies can be logged once per query (**`LogTransientErrorBody`**) — surfaces the underlying MWException trace ID that the JSON error envelope hides; rate-limit diagnostics extended (**`LogRateLimitDiagnostics`** with **`Retry-After`** / **`X-RateLimit-*`** / **`MediaWiki-API-Error`**).
- **`GameDetailImportService.ImportTeamStatsAsync`**: replaces the single brittle **`ScoreboardTeams`** Cargo query with the tiered fallback above; **`Gamelength`** intentionally excluded from all tiers (see postmortem) so we no longer pay ~15 s per import on **`internal_api_error_MWException`**; **`ParseIntOptional`** tolerates fields missing from narrower tiers.
- **`GameDetailImportService`** sides backfill: **`MatchScheduleGame`** + **`ScoreboardTeams`** (Team + Side) fallback; draft import skipped per batch when **`GameDraftEntries`** rows already exist for every game in the batch.
- **`LeaguepediaImportService`**: now binds **`IOptions<LeaguepediaClientOptions>`**; hard-coded **`Task.Delay(3000)`** / **`Task.Delay(2000)`** between tournament/match/game phases removed (covered by **`LeaguepediaClient.PostSuccessDelayMilliseconds`**); **`PreloadTeamShortNamesAsync`** and **`LookupShortNameAsync`** drop their per-region / per-lookup spacers; new private **`DelayBetweenTournamentsAsync`** uses options.
- **`LeaguepediaClientOptions`** defaults tuned for bot-authenticated traffic: **`PostSuccessDelayMilliseconds`** 2000 → **`1500`**, **`DelayBetweenGameDetailImportPhasesMilliseconds`** 5000 → **`0`**, **`DelayBetweenOngoingTournamentsMilliseconds`** 30 000 → **`5000`**.
- **`appsettings.json`** + **`appsettings.Development.json`**: Leaguepedia section reflects the new defaults and exposes **`MaxTransientRetriesPerQuery`** + **`DelayBetweenMatchImportTournamentsMilliseconds`**.
- **`Program.cs`**: shared **`CookieContainer`** singleton; **`HttpClientHandler`** uses **`UseCookies = true`** + **`CookieContainer = leaguepediaCookies`** so the MediaWiki session cookie persists across requests.

### Fixed

- **`ScoreboardTeams`** import no longer silently returns zero rows when Cargo fails after retries — the tiered fallback either returns real data from a narrower query or surfaces a clear warning. **`GameTeamStats`** is now populated for matches that previously imported only **`GamePlayerStats`** + draft.
- **`ResolveTeamStatsGameDurationSeconds`**: defaults to **`Game.Duration`** when **`Gamelength`** is missing from the winning Cargo tier (currently always, since the field is excluded — Oracle's Elixir is the planned source for real in-game duration).

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
