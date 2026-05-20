import { useId, useState } from "react";
import { importBackfillApi, type TeamBackfillResult } from "@/lib/api.ts";
import { ADMIN_LEAGUES, type AdminLeague } from "@/components/Admin/adminShared.ts";
import { formatAdminApiError } from "@/components/Admin/adminHelpers.ts";

type BackfillJob = "game-ids" | "game-sides" | "teams";

type JobResult = {
    job: BackfillJob;
    status: "idle" | "running" | "done" | "error";
    message?: string;
    teamBackfill?: TeamBackfillResult;
};

const JOB_LABELS: Record<BackfillJob, string> = {
    "game-ids": "Game external IDs",
    "game-sides": "Game sides (blue/red)",
    teams: "Team metadata",
};

function formatTeamBackfillMessage(r: TeamBackfillResult): string {
    return `${r.updated} updated, ${r.skipped} skipped, ${r.notFound} not found (${r.total} total)`;
}

export function AdminBackfillTab() {
    const ids = useId();
    const [selectedLeague, setSelectedLeague] = useState<AdminLeague>("LCK");
    const [overwrite, setOverwrite] = useState(false);
    const [running, setRunning] = useState(false);
    const [results, setResults] = useState<JobResult[]>([]);

    async function runJob(job: BackfillJob) {
        if (running || selectedLeague === "ALL") return;

        setRunning(true);
        setResults([{ job, status: "running" }]);

        try {
            if (job === "game-ids") {
                const r = await importBackfillApi.gameIds(selectedLeague);
                setResults([{
                    job,
                    status: "done",
                    message: `${r.gamesUpdated} games updated, ${r.tournamentsSkipped} tournaments skipped`,
                }]);
            } else if (job === "game-sides") {
                const r = await importBackfillApi.gameSides(selectedLeague);
                setResults([{
                    job,
                    status: "done",
                    message: `${r.gamesUpdated} games updated, ${r.tournamentsSkipped} tournaments skipped`,
                }]);
            } else {
                const r = await importBackfillApi.teams(selectedLeague, overwrite);
                setResults([{
                    job,
                    status: "done",
                    message: formatTeamBackfillMessage(r),
                    teamBackfill: r,
                }]);
            }
        } catch (err) {
            setResults([{
                job,
                status: "error",
                message: formatAdminApiError(err instanceof Error ? err.message : "Unknown error"),
            }]);
        }

        setRunning(false);
    }

    async function runAllTeamMetadata() {
        setRunning(true);
        setResults([{ job: "teams", status: "running" }]);
        try {
            const r = await importBackfillApi.teams(undefined, overwrite);
            setResults([{
                job: "teams",
                status: "done",
                message: formatTeamBackfillMessage(r),
                teamBackfill: r,
            }]);
        } catch (err) {
            setResults([{
                job: "teams",
                status: "error",
                message: formatAdminApiError(err instanceof Error ? err.message : "Unknown error"),
            }]);
        }
        setRunning(false);
    }

    const teamResult = results.find((r) => r.job === "teams");
    const missingCount = teamResult?.teamBackfill?.missingIconLogo.length ?? 0;
    const teamTotal = teamResult?.teamBackfill?.total ?? 0;

    return (
        <section className="admin__section" aria-labelledby="backfill-heading">
            <h2 id="backfill-heading" className="admin__section-title">Backfill</h2>
            <div className="admin__form">
                <p className="admin__hint">
                    Repair or enrich existing rows from Leaguepedia. These jobs do not replace a full import.
                </p>

                <fieldset className="admin__field" aria-labelledby={`${ids}-backfill-league`}>
                    <legend id={`${ids}-backfill-league`} className="admin__label">League</legend>
                    <div className="admin__league-buttons">
                        {ADMIN_LEAGUES.map((leagueCode) => (
                            <button
                                key={leagueCode}
                                type="button"
                                className={`admin__league-btn${selectedLeague === leagueCode ? " admin__league-btn--active" : ""}`}
                                onClick={() => setSelectedLeague(leagueCode)}
                                disabled={running}
                                aria-pressed={selectedLeague === leagueCode}
                            >
                                {leagueCode}
                            </button>
                        ))}
                    </div>
                    {selectedLeague === "ALL" && (
                        <p className="admin__warning">Pick a specific league for game backfills. Team metadata can run for all teams below.</p>
                    )}
                </fieldset>

                <label className="admin__step-label" htmlFor={`${ids}-overwrite`}>
                    <input
                        id={`${ids}-overwrite`}
                        type="checkbox"
                        checked={overwrite}
                        onChange={(e) => setOverwrite(e.target.checked)}
                        disabled={running}
                    />
                    Overwrite existing logo, square icon, region, short, and external id
                </label>

                <div className="admin__backfill-actions">
                    <button
                        type="button"
                        className="admin__run-btn"
                        onClick={() => runJob("game-ids")}
                        disabled={running || selectedLeague === "ALL"}
                    >
                        Game IDs
                    </button>
                    <button
                        type="button"
                        className="admin__run-btn"
                        onClick={() => runJob("game-sides")}
                        disabled={running || selectedLeague === "ALL"}
                    >
                        Game sides
                    </button>
                    <button
                        type="button"
                        className="admin__run-btn"
                        onClick={() => runJob("teams")}
                        disabled={running || selectedLeague === "ALL"}
                    >
                        Team metadata (league)
                    </button>
                    <button
                        type="button"
                        className="admin__run-btn admin__run-btn--secondary"
                        onClick={runAllTeamMetadata}
                        disabled={running}
                    >
                        Team metadata (all teams)
                    </button>
                </div>

                {results.length > 0 && (
                    <div className="admin__results-panel" role="log" aria-live="polite">
                        {results.map((result) => (
                            <div key={result.job} className="admin__results-block">
                                <div className={`admin__result admin__result--${result.status}`}>
                                    <span className="admin__result-icon" aria-hidden="true">
                                        {result.status === "running" && <span className="admin__spinner" />}
                                        {result.status === "done" && "✓"}
                                        {result.status === "error" && "✕"}
                                    </span>
                                    <span className="admin__result-title">{JOB_LABELS[result.job]}</span>
                                    {result.status !== "error" && result.message && (
                                        <span className="admin__hint">{result.message}</span>
                                    )}
                                </div>

                                {result.status === "error" && result.message && (
                                    <p className="admin__result-error-block" role="alert">
                                        {result.message}
                                    </p>
                                )}

                                {result.teamBackfill && result.status === "done" && (
                                    <div className="admin__backfill-summary">
                                        <div className="admin__backfill-stat">
                                            <span className="admin__backfill-stat-label">Missing icon URL</span>
                                            <span className={`admin__backfill-stat-value${missingCount > 0 ? " admin__backfill-stat-value--warn" : ""}`}>
                                                {missingCount}/{teamTotal}
                                            </span>
                                        </div>
                                        {missingCount > 0 && (
                                            <details className="admin__missing-icons">
                                                <summary>
                                                    {missingCount} team{missingCount === 1 ? "" : "s"} — Cargo Image has no square filename pattern
                                                </summary>
                                                <ul className="admin__missing-icons-list">
                                                    {result.teamBackfill.missingIconLogo.map((team) => (
                                                        <li key={team.id}>
                                                            <strong>{team.shortName}</strong> — {team.name}
                                                        </li>
                                                    ))}
                                                </ul>
                                            </details>
                                        )}
                                    </div>
                                )}
                            </div>
                        ))}
                    </div>
                )}
            </div>
        </section>
    );
}
