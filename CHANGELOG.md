# Changelog

All notable changes to **RiftVeil** are recorded here, **newest first**. Sections are dated (no version numbers). Where it helps, entries are grouped like [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) (Added / Changed / Fixed / Removed).

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
