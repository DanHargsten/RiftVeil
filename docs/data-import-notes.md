# Data import notes (Leaguepedia, LoLesports, Cargo)

Topic-oriented reference for pitfalls, migrations, and API behaviour. Chronological detail lives in [`technical-log.md`](technical-log.md).

## Draft import: `PicksAndBansS7` and wiki blue/red vs local `Team1` / `Team2`

Leaguepedia **`PicksAndBansS7`** exposes **`Team1*`** / **`Team2*`** columns in **wiki blue side / red side** order, **not** in local match **`Team1`** / **`Team2`** order.

If you map draft **`teamNumber`** directly from those column indices, games where local **`Team1`** played **red** get inverted draft rows relative to Leaguepedia and the rest of the app (which uses **`Team1Side`** / **`Team2Side`** consistently for player and team stats).

**Fix (2026-05-11):** map picks/bans through **`ResolveTeamNumberFromPickBan`** (same side model as other game-detail imports). Games without reliable side data are skipped (**`SkippedMissingSides`**) rather than importing wrong rows.

### Migration / existing data

After deploying the fix, stale rows may still exist for red-side **`Team1`** games. Example cleanup (adjust quoting for your database provider), then re-import game details for affected tournaments:

```sql
DELETE FROM "GameDraftEntries"
WHERE "GameId" IN (
  SELECT "Id" FROM "Games" WHERE "Team1Side" = 'Red'
);
```

Then run **`POST /api/import/game-details`** (or tournament-scoped flows) as usual.

### Related column rename (2026-04-18)

Leaguepedia moved from **`Team{n}Role{k}`** to **`Team{n}Pick{k}`** for pick columns; the importer follows the current Cargo schema.

---

## `ScoreboardTeams` tiered queries and `Gamelength`

A single wide **`ScoreboardTeams`** Cargo query was brittle: **`internal_api_error_MWException`** could burn ~15 s per import.

**Mitigation:** **`GameDetailImportService.ScoreboardTeamsCargoFieldTiers`** tries progressively **narrower** field lists until **`QueryWithOutcomeAsync`** reports success; **`ImportTeamStatsAsync`** uses the first tier that succeeds.

**`Gamelength`** was **intentionally excluded** from all tiers because it triggered the expensive server-side failures. **`ResolveTeamStatsGameDurationSeconds`** falls back to **`Game.Duration`** when Cargo does not supply length; longer-term, **Oracle's Elixir** (or similar) is the intended source for authoritative in-game duration — see [`future-projects.md`](future-projects.md).

---

## Leaguepedia client: auth, pacing, and diagnostics

- **Bot login (optional):** shared **`CookieContainer`**, **`EnsureLoggedInAsync`**, credentials from user secrets; anonymous fallback if unset.
- **Sequential Cargo:** process-wide semaphore so requests do not stampede MediaWiki.
- **`QueryWithOutcomeAsync`:** distinguishes empty success from failure-after-retries (needed for tiered fallbacks).
- **`MaxTransientRetriesPerQuery`:** caps repeated **`internal_api_error_*`** attempts so a known server bug does not dominate wall-clock time.
- **Logging:** response body buffered so transient errors can log **`MWException`** trace context; rate-limit diagnostics include **`Retry-After`**, **`X-RateLimit-*`**, **`MediaWiki-API-Error`**.
- **Name matching:** **`NameMatches`** tolerates MediaWiki disambiguation suffixes (e.g. **`"LYON (2024 American Team)"`** ↔ **`"LYON"`**) so side mapping does not silently fail.

Hard-coded delays in **`LeaguepediaImportService`** were replaced with **`LeaguepediaClientOptions`** (**`PostSuccessDelayMilliseconds`**, **`DelayBetweenMatchImportTournamentsMilliseconds`**, etc.).

---

## LoLesports VOD enrichment

- **`LolesportsVodEnricher.EnrichLeagueAsync`** pre-filters remote tournaments to those whose date range (with buffer) overlaps DB tournaments that still have games **without** a VOD, avoiding expensive **`getCompletedEvents`** calls for fully covered seasons.
- Removed extra hard-coded **`Task.Delay`** between event details and between tournaments; **`LolesportsClientOptions.RetryDelayMilliseconds`** already paces retries on transient GraphQL errors.
- Event-to-match mapping now uses two stages:
  - **strict** matching (both team codes + time window),
  - **placeholder fallback** (exactly one placeholder side, tight time window, unique candidate only).
- When fallback succeeds and the missing side can be resolved by short code, local match participants are synced before VOD attachment.
- Default VOD URL selection no longer hard-filters to `en-US`; it now prefers **English**, then **league-preferred locale prefixes**, then any remaining locale.

---

## `GameVod.Source`: imported vs manual rows

Manual VOD overrides used to be encoded as `Locale = "manual"`. This made dedupe and selection logic brittle and mixed origin semantics into locale text.

### Schema and domain change

- Added enum **`VodSource`** (`Imported`, `Manual`) on **`GameVod`**.
- Unique index changed from `(GameId, Provider, Locale)` to `(GameId, Provider, Locale, Source)`.
- Domain add/remove/manual-selection logic now keys on `Source` instead of magic locale values.

### Migration (`AddGameVodSource`)

For existing rows:

- add `Source` with default `Imported`,
- migrate historical manual rows (`Locale = 'manual'`) to `Source = Manual` and set `Locale = NULL`,
- recreate the unique index with the new key shape.

Apply migration **before** running mixed import + manual VOD workflows in non-empty databases.

---

## Game detail import filters

- **`ImportGameDetailsForTournamentAsync`** only processes games with **`Game.WinningTeam != null`** so unplayed phantom bracket games (e.g. undecided BO3 game 3) do not trigger Cargo round-trips.

---

## Team metadata: Cargo `Teams.Image`, `LogoUrl`, and `IconLogoUrl`

Leaguepedia stores a single **`Teams.Image`** filename (often a wordmark, e.g. `FlyQuestlogo profile.png`). RiftVeil maps it as follows:

- **`Team.LogoUrl`** — `Special:FilePath` URL for the Cargo filename as-is (wordmark/profile).
- **`Team.IconLogoUrl`** — square/isotype when Leaguepedia naming allows (`*logo square.png`), derived from the wordmark via **`LeaguepediaImageUrls.ToSquareLogoFileName`**, optionally verified with **`LeaguepediaClient.FilePathUrlExistsAsync`** (same bot session as Cargo).

Import and **`BackfillTeamMetadataAsync`** preload Cargo **`Teams`** rows (broader region list than the old short-name-only cache). Teams with no square filename pattern are reported in **`TeamBackfillResultDto.MissingIconLogo`** for manual follow-up in Admin.

### Migration

Apply **`AddTeamIconLogoUrl`** before running team backfill or opening the Admin Teams tab. Existing DBs keep working for reads once migrated.

### Frontend resolution order (icon variant)

1. `frontend/public/logos/teams/{short}-square.png`
2. `{short}.png`
3. `IconLogoUrl` from API
4. `placeholder.png`

Wordmarks use `{short}.png` → `LogoUrl` → placeholder. See **`frontend/src/lib/teamLogo.ts`**.

---

## Scoped import windows (`ImportTournamentFilter`)

**`ImportTournamentFilter.DefaultRecentDays`** (7) drives **recent** match/VOD/game-detail jobs: tournaments whose schedule overlaps the last N days (ongoing + just finished). Ongoing-only paths use **`WhereOngoing`** / **`WhereOngoingByStatus`** depending on the job. Tournament list import still loads the full league from Leaguepedia regardless of match scope.
