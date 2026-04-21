import { useState, useId } from "react";

const LEAGUES = ["ALL", "LEC", "LCS", "LCK"] as const;
type League = typeof LEAGUES[number];

type Step = "tournaments" | "matches" | "vods" | "game-details";

type StepResult = {
    step: Step;
    status: "idle" | "running" | "done" | "error";
    message?: string;
};

const STEP_LABELS: Record<Step, string> = {
    tournaments: "Tournaments",
    matches: "Matches",
    vods: "VODs",
    "game-details": "Game Details (all ongoing)",
};

async function runImport(league: League, step: Step, ongoingOnly: boolean): Promise<void> {
    // Game details runs once for all ongoing tournaments, no league param needed
    if (step === "game-details") {
        const res = await fetch("/api/import/game-details/ongoing", { method: "POST" });
        if (!res.ok) {
            const text = await res.text();
            throw new Error(text || res.statusText);
        }
        return;
    }

    const leagues = league === "ALL" ? ["LEC", "LCS", "LCK"] : [league];
    for (const l of leagues) {
        const endpoint = step === "matches" && ongoingOnly
            ? `matches/${l}/ongoing`
            : `${step}/${l}`;
        const res = await fetch(`/api/import/${endpoint}`, { method: "POST" });
        if (!res.ok) {
            const text = await res.text();
            throw new Error(`${l}: ${text || res.statusText}`);
        }
    }
}

export function Admin() {
    const ids = useId();
    const leagueFieldId = `${ids}-league`;
    const stepsFieldId = `${ids}-steps`;
    const resultsId = `${ids}-results`;

    const [selectedLeague, setSelectedLeague] = useState<League>("LEC");
    const [selectedSteps, setSelectedSteps] = useState<Set<Step>>(new Set(["tournaments", "matches"]));
    const [ongoingOnly, setOngoingOnly] = useState(false);
    const [results, setResults] = useState<StepResult[]>([]);
    const [running, setRunning] = useState(false);

    function toggleStep(step: Step) {
        setSelectedSteps(prev => {
            const next = new Set(prev);
            next.has(step) ? next.delete(step) : next.add(step);
            return next;
        });
    }

    async function handleRun() {
        if (running || selectedSteps.size === 0) return;
        setRunning(true);

        const steps = (["tournaments", "matches", "vods", "game-details"] as Step[]).filter(s => selectedSteps.has(s));
        setResults(steps.map(s => ({ step: s, status: "idle" })));

        for (const step of steps) {
            setResults(prev => prev.map(r => r.step === step ? { ...r, status: "running" } : r));
            try {
                await runImport(selectedLeague, step, ongoingOnly);
                setResults(prev => prev.map(r => r.step === step ? { ...r, status: "done" } : r));
            } catch (err) {
                const msg = err instanceof Error ? err.message : "Unknown error";
                setResults(prev => prev.map(r => r.step === step ? { ...r, status: "error", message: msg } : r));
                break;
            }
        }

        setRunning(false);
    }

    return (
        <div className="page">
            <div className="admin">
                <h1 className="admin__title">Admin</h1>

                {/* ── IMPORT SECTION ── */}
                <section className="admin__section" aria-labelledby="import-heading">
                    <h2 id="import-heading" className="admin__section-title">Import data</h2>

                    <div className="admin__form">

                        {/* League */}
                        <fieldset className="admin__field" aria-labelledby={leagueFieldId}>
                            <legend id={leagueFieldId} className="admin__label">League</legend>
                            <div className="admin__league-buttons">
                                {LEAGUES.map(l => (
                                    <button
                                        key={l}
                                        type="button"
                                        className={`admin__league-btn${selectedLeague === l ? " admin__league-btn--active" : ""}`}
                                        onClick={() => setSelectedLeague(l)}
                                        disabled={running}
                                        aria-pressed={selectedLeague === l}
                                    >
                                        {l}
                                    </button>
                                ))}
                            </div>
                        </fieldset>

                        {/* Steps */}
                        <fieldset className="admin__field" aria-labelledby={stepsFieldId}>
                            <legend id={stepsFieldId} className="admin__label">Steps</legend>
                            <div className="admin__steps">
                                {(["tournaments", "matches", "vods", "game-details"] as Step[]).map(step => (
                                    <label key={step} className="admin__step-label">
                                        <input
                                            type="checkbox"
                                            checked={selectedSteps.has(step)}
                                            onChange={() => toggleStep(step)}
                                            disabled={running}
                                        />
                                        {STEP_LABELS[step]}
                                    </label>
                                ))}
                            </div>

                            {/* Only ongoing — shown when Matches is selected */}
                            {selectedSteps.has("matches") && (
                                <label className="admin__step-label admin__step-label--sub">
                                    <input
                                        type="checkbox"
                                        checked={ongoingOnly}
                                        onChange={() => setOngoingOnly(o => !o)}
                                        disabled={running}
                                    />
                                    Only ongoing tournaments
                                </label>
                            )}
                        </fieldset>

                        <button
                            type="button"
                            className="admin__run-btn"
                            onClick={handleRun}
                            disabled={running || selectedSteps.size === 0}
                            aria-busy={running}
                        >
                            {running ? "Running…" : `Run for ${selectedLeague}`}
                        </button>

                        {/* Results */}
                        {results.length > 0 && (
                            <div
                                id={resultsId}
                                className="admin__results"
                                role="log"
                                aria-live="polite"
                                aria-relevant="additions"
                                aria-label="Import results"
                            >
                                {results.map(r => (
                                    <div key={r.step} className={`admin__result admin__result--${r.status}`}>
                                        <span className="admin__result-icon" aria-hidden="true">
                                            {r.status === "running" && <span className="admin__spinner" />}
                                            {r.status === "done" && "✓"}
                                            {r.status === "error" && "✕"}
                                            {r.status === "idle" && "·"}
                                        </span>
                                        <span>{STEP_LABELS[r.step]}</span>
                                        {r.status === "error" && r.message && (
                                            <span className="admin__result-error" role="alert">{r.message}</span>
                                        )}
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>
                </section>

                {/* ── PLACEHOLDER SECTIONS ── */}
                <section className="admin__section" aria-labelledby="matches-heading">
                    <h2 id="matches-heading" className="admin__section-title">Match management</h2>
                    <p className="admin__placeholder">Edit match timestamps, correct results, and more — coming soon.</p>
                </section>

                <section className="admin__section" aria-labelledby="users-heading">
                    <h2 id="users-heading" className="admin__section-title">Users</h2>
                    <p className="admin__placeholder">Manage admin access — coming soon.</p>
                </section>
            </div>
        </div>
    );
}