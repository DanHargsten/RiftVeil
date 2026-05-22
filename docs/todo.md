# TODO (small fixes / bugs)

Short-term list for practical fixes that are smaller than the scoped items in `future-projects.md`.

## UI / UX

- [ ] **Mobile / responsive layout (prio):** Frontend targets desktop only for now; viewport `@media` rules were removed. Reintroduce breakpoints for match detail, damage breakdown, sidebar, home, and match list when prioritised.
- [ ] Sidebar: when a league has many tournaments, make the list scrollable instead of clipping.
- [ ] Sidebar structure: group tournaments by year (`LEC -> 2026, 2025, 2024`) before split/stage labels.
- [ ] Sidebar readability: avoid very long flat lists like `2026 Spring Playoffs, 2026 Spring, 2026 Versus...`.
- [ ] Match search behavior: when searching a team tag (e.g. `GEN`), fetch and show more played matches from backend instead of only filtering the homepage preloaded list. Keep current default view when search is empty, and only expand search when the user has typed at least 2 characters.

## Import / data flow

- [x] Add backfill actions (`game ids`, `game sides`, `team metadata`) as explicit admin actions — **Backfill** tab at `/admin`.
- [ ] Improve admin helper text around Game Details prerequisites (`played games`, `ExternalId`).

## Media / logos

- [x] Team `logoUrl` / `iconLogoUrl` from Leaguepedia + local `{short}-square.png` / `{short}.png` fallback chain in match UI.
- [ ] League `logoUrl` in API (local league PNGs still primary).
- [x] Add `frontend/public/logos/leagues/cblol.png` and `lcp.png`.
- [ ] **TBD team logo:** add/fix a placeholder logo for undetermined bracket slots (TBD opponents) so match cards and detail pages don't show a broken or wrong fallback.
