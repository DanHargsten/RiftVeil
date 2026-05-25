import { useId, useState } from "react";
import { importBackfillApi, type TeamBackfillResult } from "@/lib/api.ts";
import { ADMIN_LEAGUES, type AdminLeague } from "@/components/Admin/adminShared.ts";
import { formatAdminApiError } from "@/components/Admin/adminHelpers.ts";

type JobResult = {
    status: "idle" | "running" | "done" | "error";
    message?: string;
    teamBackfill?: TeamBackfillResult;
};

function formatTeamBackfillMessage(result: TeamBackfillResult): string {
    return `${result.updated} updated, ${result.skipped} skipped, ${result.notFound} not found (${result.total} total)`;
}

export function AdminBackfillTab() {
    const ids = useId();
    const [selectedLeague, setSelectedLeague] = useState<AdminLeague>("LCK");
    const [overwrite, setOverwrite] = useState(false);
    const [running, setRunning] = useState(false);
    const [result, setResult] = useState<JobResult | null>(null);

    async function runTeamMetadataForLeague() {
        if (running || selectedLeague === "ALL") return;

        setRunning(true);
        setResult({ status: "running" });

        try {
            const teamBackfill = await importBackfillApi.teams(selectedLeague, overwrite);
            setResult({
                status: "done",
                message: formatTeamBackfillMessage(teamBackfill),
                teamBackfill,
            });
        } catch (err) {
            setResult({
                status: "error",
                message: formatAdminApiError(err instanceof Error ? err.message : "Unknown error"),
            });
        }

        setRunning(false);
    }

    async function runAllTeamMetadata() {
        setRunning(true);
        setResult({ status: "running" });
        try {
            const teamBackfill = await importBackfillApi.teams(undefined, overwrite);
            setResult({
                status: "done",
                message: formatTeamBackfillMessage(teamBackfill),
                teamBackfill,
            });
        } catch (err) {
            setResult({
                status: "error",
                message: formatAdminApiError(err instanceof Error ? err.message : "Unknown error"),
            });
        }
        setRunning(false);
    }

    const missingCount = result?.teamBackfill?.missingIconLogo.length ?? 0;
    const teamTotal = result?.teamBackfill?.total ?? 0;

    return (
        <section className="admin__section" aria-labelledby="backfill-heading">
            <h2 id="backfill-heading" className="admin__section-title">Repair</h2>
            <div className="admin__form">
                <p className="admin__hint">
                    Match import now sets Leaguepedia GameId, blue/red sides, and team metadata automatically.
                    Use this tab only to force-refresh team logos and Cargo fields (optional overwrite).
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
                        <p className="admin__warning">Pick a specific league for league-scoped repair, or use all teams below.</p>
                    )}
                </fieldset>

                <label className="admin__step-label" htmlFor={`${ids}-overwrite`}>
                    <input
                        id={`${ids}-overwrite`}
                        type="checkbox"
                        checked={overwrite}
                        onChange={(event) => setOverwrite(event.target.checked)}
                        disabled={running}
                    />
                    Overwrite existing logo, square icon, region, short, and external id
                </label>

                <div className="admin__backfill-actions">
                    <button
                        type="button"
                        className="admin__run-btn"
                        onClick={runTeamMetadataForLeague}
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

                {result && (
                    <div className="admin__results-panel" role="log" aria-live="polite">
                        <div className="admin__results-block">
                            <div className={`admin__result admin__result--${result.status}`}>
                                <span className="admin__result-icon" aria-hidden="true">
                                    {result.status === "running" && <span className="admin__spinner" />}
                                    {result.status === "done" && "✓"}
                                    {result.status === "error" && "✕"}
                                </span>
                                <span className="admin__result-title">Team metadata</span>
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
                    </div>
                )}
            </div>
        </section>
    );
}
