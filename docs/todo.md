# TODO (small fixes / bugs)

Short-term list for practical fixes that are smaller than the scoped items in `future-projects.md`.

## UI / UX

- [ ] Sidebar: when a league has many tournaments, make the list scrollable instead of clipping.
- [ ] Sidebar structure: group tournaments by year (`LEC -> 2026, 2025, 2024`) before split/stage labels.
- [ ] Sidebar readability: avoid very long flat lists like `2026 Spring Playoffs, 2026 Spring, 2026 Versus...`.

## Import / data flow

- [ ] Add backfill actions (`game ids`, `game sides`) as explicit admin actions.
- [ ] Improve admin helper text around Game Details prerequisites (`played games`, `ExternalId`).

## Media / logos

- [ ] Start migrating to real `logoUrl` usage (league/team first, local files as fallback).
