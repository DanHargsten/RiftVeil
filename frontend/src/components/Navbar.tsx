import { useState, useRef, useEffect } from "react";
import { NavLink } from "react-router-dom";

const LEAGUES = ["ALL", "LEC", "LCS", "LCK"] as const;
type League = typeof LEAGUES[number];

type Step = "tournaments" | "matches" | "vods";

type StepResult = {
    step: Step;
    status: "idle" | "running" | "done" | "error";
    message?: string;
};

const STEP_LABELS: Record<Step, string> = {
    tournaments: "Tournaments",
    matches: "Matches",
    vods: "VODs",
};

async function runImport(league: League, step: Step, ongoingOnly: boolean): Promise<void> {
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

export function Navbar() {
    const [menuOpen, setMenuOpen] = useState(false);
    const [selectedLeague, setSelectedLeague] = useState<League>("LEC");
    const [selectedSteps, setSelectedSteps] = useState<Set<Step>>(new Set(["tournaments", "matches"]));
    const [ongoingOnly, setOngoingOnly] = useState(false);
    const [results, setResults] = useState<StepResult[]>([]);
    const [running, setRunning] = useState(false);
    const menuRef = useRef<HTMLDivElement>(null);

    // Close on outside click
    useEffect(() => {
        if (!menuOpen) return;
        const handler = (e: MouseEvent) => {
            if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
                setMenuOpen(false);
            }
        };
        document.addEventListener("mousedown", handler);
        return () => document.removeEventListener("mousedown", handler);
    }, [menuOpen]);

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

        const steps = (["tournaments", "matches", "vods"] as Step[]).filter(s => selectedSteps.has(s));
        const initial: StepResult[] = steps.map(s => ({ step: s, status: "idle" }));
        setResults(initial);

        for (let i = 0; i < steps.length; i++) {
            const step = steps[i];
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

    function handleOpen() {
        setMenuOpen(o => !o);
        setResults([]);
    }

    return (
        <nav className="navbar" aria-label="Primary">
            <div className="navbar__container container">
                <NavLink to="/" className="navbar__brand">
                    Rift Veil
                </NavLink>

                <div className="navbar__links">
                    <NavLink to="/" end className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Home
                    </NavLink>
                    <NavLink to="/tournaments" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Tournaments
                    </NavLink>
                    <NavLink to="/leagues" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Leagues
                    </NavLink>
                    <NavLink to="/standings" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Standings
                    </NavLink>
                    <NavLink to="/teams" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Teams
                    </NavLink>
                </div>

                {/* Import menu */}
                <div className="import-menu" ref={menuRef}>
                    <button
                        className={`import-menu__trigger${menuOpen ? " import-menu__trigger--open" : ""}`}
                        onClick={handleOpen}
                        aria-label="Import menu"
                        title="Import data"
                    >
                        <span /><span /><span />
                    </button>

                    {menuOpen && (
                        <div className="import-menu__panel">
                            <p className="import-menu__heading">Import data</p>

                            {/* League selector */}
                            <div className="import-menu__field">
                                <label className="import-menu__label">League</label>
                                <div className="import-menu__league-buttons">
                                    {LEAGUES.map(l => (
                                        <button
                                            key={l}
                                            className={`import-menu__league-btn${selectedLeague === l ? " import-menu__league-btn--active" : ""}`}
                                            onClick={() => setSelectedLeague(l)}
                                            disabled={running}
                                        >
                                            {l}
                                        </button>
                                    ))}
                                </div>
                            </div>

                            {/* Step checkboxes */}
                            <div className="import-menu__field">
                                <label className="import-menu__label">Steps</label>
                                <div className="import-menu__steps">
                                    {(["tournaments", "matches", "vods"] as Step[]).map(step => (
                                        <label key={step} className="import-menu__step-label">
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
                            </div>

                            {selectedSteps.has("matches") && (
                                <label className="import-menu__step-label">
                                    <input
                                        type="checkbox"
                                        checked={ongoingOnly}
                                        onChange={() => setOngoingOnly(o => !o)}
                                        disabled={running}
                                    />
                                    Only ongoing
                                </label>
                            )}

                            {/* Results */}
                            {results.length > 0 && (
                                <div className="import-menu__results">
                                    {results.map(r => (
                                        <div key={r.step} className={`import-menu__result import-menu__result--${r.status}`}>
                                            <span className="import-menu__result-icon">
                                                {r.status === "running" && <span className="import-menu__spinner" />}
                                                {r.status === "done" && "✓"}
                                                {r.status === "error" && "✕"}
                                                {r.status === "idle" && "·"}
                                            </span>
                                            <span>{STEP_LABELS[r.step]}</span>
                                            {r.status === "error" && r.message && (
                                                <span className="import-menu__result-error">{r.message}</span>
                                            )}
                                        </div>
                                    ))}
                                </div>
                            )}

                            <button
                                className="import-menu__run-btn"
                                onClick={handleRun}
                                disabled={running || selectedSteps.size === 0}
                            >
                                {running ? "Running…" : `Run for ${selectedLeague}`}
                            </button>
                        </div>
                    )}
                </div>
            </div>
        </nav>
    );
}