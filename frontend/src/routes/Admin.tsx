import { useQuery } from "@tanstack/react-query";
import { useId, useMemo, useState } from "react";
import { tournamentsApi } from "@/lib/api.ts";

const LEAGUES = ["ALL", "LEC", "LCS", "LCK", "LPL"] as const;
type League = typeof LEAGUES[number];

type Step = "tournaments" | "matches" | "vods" | "game-details";
type ImportScope = "all" | "ongoing";
type GameDetailsMode = "ongoing" | "tournament";

type StepResult = {
    step: Step;
    status: "idle" | "running" | "done" | "error";
    message?: string;
};

const STEP_LABELS: Record<Step, string> = {
    tournaments: "Tournaments",
    matches: "Matches",
    vods: "VODs",
    "game-details": "Game Details",
};

async function runImport(
    league: League,
    step: Step,
    importScope: ImportScope,
    gameDetailsMode: GameDetailsMode,
    gameDetailsTournamentId: number | null,
): Promise<void> {
    const leagues = league === "ALL" ? ["LEC", "LCS", "LCK", "LPL"] : [league];

    if (step === "game-details") {
        if (gameDetailsMode === "tournament") {
            if (gameDetailsTournamentId == null) {
                throw new Error("Pick a tournament for Game Details import.");
            }
            const res = await fetch(`/api/import/game-details/tournament/${gameDetailsTournamentId}`, { method: "POST" });
            if (!res.ok) {
                const text = await res.text();
                throw new Error(text || res.statusText);
            }
            return;
        }

        for (const leagueCode of leagues) {
            const res = await fetch(`/api/import/game-details/${leagueCode}?ongoingOnly=true`, { method: "POST" });
            if (!res.ok) {
                const text = await res.text();
                throw new Error(`${leagueCode}: ${text || res.statusText}`);
            }
        }
        return;
    }

    for (const leagueCode of leagues) {
        const endpoint = step === "matches"
            ? (importScope === "ongoing" ? `matches/${leagueCode}/ongoing` : `matches/${leagueCode}`)
            : step === "vods"
                ? (importScope === "ongoing" ? `vods/${leagueCode}/ongoing` : `vods/${leagueCode}`)
                : `${step}/${leagueCode}`;
        const res = await fetch(`/api/import/${endpoint}`, { method: "POST" });
        if (!res.ok) {
            const text = await res.text();
            throw new Error(`${leagueCode}: ${text || res.statusText}`);
        }
    }
}

export function Admin() {
    const ids = useId();
    const leagueFieldId = `${ids}-league`;
    const stepsFieldId = `${ids}-steps`;
    const importScopeFieldId = `${ids}-import-scope`;
    const gameDetailsScopeFieldId = `${ids}-game-details-scope`;
    const gameDetailsTournamentFieldId = `${ids}-game-details-tournament`;
    const resultsId = `${ids}-results`;

    const [selectedLeague, setSelectedLeague] = useState<League>("LEC");
    const [selectedSteps, setSelectedSteps] = useState<Set<Step>>(new Set(["tournaments", "matches"]));
    const [importScope, setImportScope] = useState<ImportScope>("all");
    const [gameDetailsMode, setGameDetailsMode] = useState<GameDetailsMode>("ongoing");
    const [gameDetailsTournamentId, setGameDetailsTournamentId] = useState<number | null>(null);
    const [results, setResults] = useState<StepResult[]>([]);
    const [running, setRunning] = useState(false);

    const { data: tournaments } = useQuery({
        queryKey: ["tournaments"],
        queryFn: () => tournamentsApi.getAll(),
    });

    const gameDetailsTournamentOptions = useMemo(() => {
        const list = tournaments ?? [];
        const filtered = selectedLeague === "ALL"
            ? list
            : list.filter((tournament) => tournament.leagueShortName === selectedLeague);

        return [...filtered].sort(
            (a, b) => new Date(b.startsAtUtc).getTime() - new Date(a.startsAtUtc).getTime(),
        );
    }, [tournaments, selectedLeague]);

    function toggleStep(step: Step) {
        setSelectedSteps(prev => {
            const next = new Set(prev);
            next.has(step) ? next.delete(step) : next.add(step);
            return next;
        });
    }

    async function handleRun() {
        if (running || selectedSteps.size === 0) return;

        if (selectedSteps.has("game-details") && gameDetailsMode === "tournament" && gameDetailsTournamentId == null) {
            setResults([
                {
                    step: "game-details",
                    status: "error",
                    message: "Choose a tournament for Game Details import.",
                },
            ]);
            return;
        }

        setRunning(true);

        const steps = (["tournaments", "matches", "vods", "game-details"] as Step[]).filter((step) =>
            selectedSteps.has(step),
        );
        setResults(steps.map((step) => ({ step, status: "idle" })));

        for (const step of steps) {
            setResults((prev) => prev.map((result) => (result.step === step ? { ...result, status: "running" } : result)));
            try {
                await runImport(selectedLeague, step, importScope, gameDetailsMode, gameDetailsTournamentId);
                setResults((prev) => prev.map((result) => (result.step === step ? { ...result, status: "done" } : result)));
            } catch (err) {
                const msg = err instanceof Error ? err.message : "Unknown error";
                setResults((prev) =>
                    prev.map((result) =>
                        result.step === step ? { ...result, status: "error", message: msg } : result,
                    ),
                );
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
                                {LEAGUES.map((leagueCode) => (
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
                        </fieldset>

                        {(selectedSteps.has("tournaments") || selectedSteps.has("matches") || selectedSteps.has("vods")) && (
                            <fieldset className="admin__field" aria-labelledby={importScopeFieldId}>
                                <legend id={importScopeFieldId} className="admin__label">Import scope</legend>
                                <div className="admin__radio-group">
                                    <label className="admin__step-label">
                                        <input
                                            type="radio"
                                            name={`${ids}-import-scope`}
                                            checked={importScope === "all"}
                                            onChange={() => setImportScope("all")}
                                            disabled={running}
                                        />
                                        All tournaments
                                    </label>
                                    <label className="admin__step-label">
                                        <input
                                            type="radio"
                                            name={`${ids}-import-scope`}
                                            checked={importScope === "ongoing"}
                                            onChange={() => setImportScope("ongoing")}
                                            disabled={running}
                                        />
                                        Only ongoing tournaments
                                    </label>
                                </div>
                                {selectedSteps.has("tournaments") && importScope === "ongoing" && (
                                    <p className="admin__hint">
                                        Tournament refresh has no strict ongoing endpoint yet; it still refreshes latest league tournaments.
                                    </p>
                                )}
                            </fieldset>
                        )}

                        {selectedSteps.has("game-details") && (
                            <fieldset className="admin__field" aria-labelledby={gameDetailsScopeFieldId}>
                                <legend id={gameDetailsScopeFieldId} className="admin__label">Game details scope</legend>
                                <div className="admin__radio-group">
                                    <label className="admin__step-label">
                                        <input
                                            type="radio"
                                            name={`${ids}-game-details-scope`}
                                            checked={gameDetailsMode === "ongoing"}
                                            onChange={() => setGameDetailsMode("ongoing")}
                                            disabled={running}
                                        />
                                        Ongoing tournaments only (recommended)
                                    </label>
                                    <label className="admin__step-label">
                                        <input
                                            type="radio"
                                            name={`${ids}-game-details-scope`}
                                            checked={gameDetailsMode === "tournament"}
                                            onChange={() => setGameDetailsMode("tournament")}
                                            disabled={running}
                                        />
                                        Specific tournament
                                    </label>
                                </div>

                                {gameDetailsMode === "tournament" && (
                                    <>
                                        <label htmlFor={gameDetailsTournamentFieldId} className="admin__hint">
                                            Tournament
                                        </label>
                                        <select
                                            id={gameDetailsTournamentFieldId}
                                            className="admin__select"
                                            value={gameDetailsTournamentId ?? ""}
                                            onChange={(e) =>
                                                setGameDetailsTournamentId(
                                                    e.target.value ? Number(e.target.value) : null,
                                                )
                                            }
                                            disabled={running}
                                        >
                                            <option value="">Select a tournament...</option>
                                            {gameDetailsTournamentOptions.map((tournament) => (
                                                <option key={tournament.id} value={tournament.id}>
                                                    {selectedLeague === "ALL"
                                                        ? `${tournament.leagueShortName} — ${tournament.name}`
                                                        : tournament.name}
                                                </option>
                                            ))}
                                        </select>
                                    </>
                                )}

                                {gameDetailsMode === "tournament" && (
                                    <p className="admin__warning">
                                        Warning: Tournament-level Game Details can still be heavy, but much safer than full-league historical imports.
                                    </p>
                                )}
                            </fieldset>
                        )}

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
                                {results.map((result) => (
                                    <div key={result.step} className={`admin__result admin__result--${result.status}`}>
                                        <span className="admin__result-icon" aria-hidden="true">
                                            {result.status === "running" && <span className="admin__spinner" />}
                                            {result.status === "done" && "✓"}
                                            {result.status === "error" && "✕"}
                                            {result.status === "idle" && "·"}
                                        </span>
                                        <span>{STEP_LABELS[result.step]}</span>
                                        {result.status === "error" && result.message && (
                                            <span className="admin__result-error" role="alert">{result.message}</span>
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