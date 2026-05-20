# RiftVeil

RiftVeil is a spoiler-aware League of Legends esports web app: one place to see schedules and series across leagues, with optional reveal of results and links to broadcasts and VODs.

## What it does today

- **Home** — Live, upcoming, and recent matches with global and per-match spoiler controls (`useSpoilerPrefs`). Live matches show a red accent stripe, a pulsing badge, and a direct **Watch live** link to lolesports.com.
- **Leagues** — Hub per league at `/leagues/:shortName`: pick a tournament, browse matches grouped by round, open a series without leaving the league context.
- **Match detail** — Series scoreline, per-game tabs (played games only), VOD link when available, per-game **draft** (`GameDraft`) and **scoreboard** (`GameScoreboard`) from `GET /api/games/{gameId}/details`, and a **global objectives** column (towers, dragons, baron, void grubs) beside the main column.
- **Admin** — At `/admin`, three tabs: **Import** (tournaments, matches, VODs, game details with league + scope), **Backfill** (game external IDs, blue/red sides, team metadata from Leaguepedia), and **Teams** (inspect/edit/sync logo URLs and shorts). There is no admin link in the navbar.

## Data flow

- **Leaguepedia** — Primary source for tournaments, matches, games, team metadata (logo/wordmark + square icon URLs from Cargo `Teams.Image`), and related metadata via the import API.
- **Lolesports** — Used to enrich games with VOD URLs where possible (LEC, LCS, LCK, LPL, CBLOL, LCP).

## Tech stack

| Layer    | Choice |
|----------|--------|
| Backend | ASP.NET Core Web API, EF Core, SQL Server |
| Frontend | React 19, TypeScript, Vite, React Router, TanStack Query |

## Repository layout

- `backend/` — .NET solution (`RiftVeil.sln`): API, domain, infrastructure, tests.
- `frontend/` — Vite + React client; dev server proxies `/api` to the backend.

## External services

The import pipeline depends on two external sources. Configure these via
**user secrets** (development) or environment variables (production):

- `Leaguepedia:BotUsername` / `Leaguepedia:BotPassword` — bot credentials
  for [Leaguepedia](https://lol.fandom.com/) (raises Cargo API rate limits
  vs anonymous access). Optional: anonymous fallback works for most reads.
- `Lolesports:ApiKey` — public x-api-key used against Riot's lolesports
  GraphQL gateway for VOD enrichment.

This is a personal portfolio project; credentials are not bundled. To run
the importers you need your own Leaguepedia bot account and the public
lolesports key (commonly known and rotated periodically).

## Development (local)

1. **Database** — Ensure SQL Server is available. The API’s `appsettings.Development.json` defaults to LocalDB (`RiftVeil` database); adjust the connection string if you use another instance.
2. **Backend** — From `backend/RiftVeil.Api` (or the solution root), apply EF migrations (includes `Teams.IconLogoUrl`) and run the API (HTTP profile listens on **5133** in development so it matches the Vite proxy).
3. **Frontend** — From `frontend/`, run `npm install` once, then `npm run dev` (opens the app, typically on port **5173**). Copy **`frontend/.env.example`** to **`.env.local`** if you want to set optional vars (e.g. **`VITE_CONTACT_EMAIL`** for the footer mailto link).

## Status

MVP-focused: solid import pipeline and browsing experience first; richer in-match stats UI and extras (for example reminders) come later. See [`CHANGELOG.md`](CHANGELOG.md) for a short dated summary, [`docs/technical-log.md`](docs/technical-log.md) for detailed development notes, and [`docs/data-import-notes.md`](docs/data-import-notes.md) for import-specific pitfalls and migrations.
