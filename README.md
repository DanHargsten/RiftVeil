# RiftVeil

RiftVeil is a spoiler-aware League of Legends esports web app: one place to see schedules and series across leagues, with optional reveal of results and links to broadcasts and VODs.

## What it does today

- **Home** — Live, upcoming, and recent matches with global and per-match spoiler controls (`useSpoilerPrefs`). Live matches show a red accent stripe, a pulsing badge, and a direct **Watch live** link to lolesports.com.
- **Leagues** — Hub per league at `/leagues/:shortName`: pick a tournament, browse matches grouped by round, open a series without leaving the league context.
- **Match detail** — Series scoreline, per-game tabs (played games only), VOD link when available, plus per-game **draft** (`GameDraft`) and **scoreboard** (`GameScoreboard`) backed by `GET /api/games/{gameId}/details`. Objectives sidebar still a placeholder while the presentational component is built.
- **Admin** — At `/admin`, run import jobs (tournaments, matches, VOD enrichment, per-tournament and per-game detail import) that fill the database from external sources.

## Data flow

- **Leaguepedia** — Primary source for tournaments, matches, games, and related metadata via the import API.
- **Lolesports** — Used to enrich games with VOD URLs where possible.

## Tech stack

| Layer    | Choice |
|----------|--------|
| Backend | ASP.NET Core Web API, EF Core, SQL Server |
| Frontend | React 19, TypeScript, Vite, React Router, TanStack Query |

## Repository layout

- `backend/` — .NET solution (`RiftVeil.sln`): API, domain, infrastructure, tests.
- `frontend/` — Vite + React client; dev server proxies `/api` to the backend.

## Development (local)

1. **Database** — Ensure SQL Server is available. The API’s `appsettings.Development.json` defaults to LocalDB (`RiftVeil` database); adjust the connection string if you use another instance.
2. **Backend** — From `backend/RiftVeil.Api` (or the solution root), apply migrations and run the API (HTTP profile listens on **5133** in development so it matches the Vite proxy).
3. **Frontend** — From `frontend/`, run `npm install` once, then `npm run dev` (opens the app, typically on port **5173**).

## Status

MVP-focused: solid import pipeline and browsing experience first; richer in-match stats UI and extras (for example reminders) come later. See [`CHANGELOG.md`](CHANGELOG.md) for a dated history of changes.
