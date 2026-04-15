import { useState, useRef, useEffect, useId } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { matchesApi, leaguesApi } from "@/lib/api.ts";
import { LeagueLogo } from "@/components/common/Logos.tsx";

export function Navbar() {
    const leagueMenuRef = useRef<HTMLDivElement>(null);
    const leagueTriggerRef = useRef<HTMLButtonElement>(null);
    const [leagueMenuOpen, setLeagueMenuOpen] = useState(false);
    const navigate = useNavigate();
    const navIds = useId();
    const leagueListId = `${navIds}-league-list`;

    const { data: leagues } = useQuery({
        queryKey: ["leagues"],
        queryFn: () => leaguesApi.getAll(),
    });

    const { data: liveMatches } = useQuery({
        queryKey: ["matches", "live"],
        queryFn: () => matchesApi.getLive(),
        refetchInterval: 30_000,
    });
    const liveCount = liveMatches?.length ?? 0;

    // Stäng leagues-dropdown vid klick utanför
    useEffect(() => {
        if (!leagueMenuOpen) return;
        const handler = (e: MouseEvent) => {
            if (leagueMenuRef.current && !leagueMenuRef.current.contains(e.target as Node)) {
                setLeagueMenuOpen(false);
            }
        };
        document.addEventListener("mousedown", handler);
        return () => document.removeEventListener("mousedown", handler);
    }, [leagueMenuOpen]);

    // Stäng med Escape och återställ fokus till trigger-knappen
    useEffect(() => {
        if (!leagueMenuOpen) return;
        const handler = (e: KeyboardEvent) => {
            if (e.key === "Escape") {
                setLeagueMenuOpen(false);
                leagueTriggerRef.current?.focus();
            }
        };
        document.addEventListener("keydown", handler);
        return () => document.removeEventListener("keydown", handler);
    }, [leagueMenuOpen]);

    function closeLeagueMenu() {
        setLeagueMenuOpen(false);
        leagueTriggerRef.current?.focus();
    }

    return (
        <nav className="navbar" aria-label="Primary">
            <div className="navbar__container container">

                {/* BRAND */}
                <NavLink to="/" className="navbar__brand">
                    Rift Veil
                </NavLink>

                {/* ========== PRIMARY LINKS ========== */}
                <div className="navbar__links">
                    <NavLink to="/" end className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Home
                    </NavLink>

                    {/* LEAGUES DROPDOWN */}
                    <div className="navbar__league-menu" ref={leagueMenuRef}>
                        <button
                            ref={leagueTriggerRef}
                            type="button"
                            className={`navbar__link navbar__league-trigger${leagueMenuOpen ? " navbar__link--active" : ""}`}
                            onClick={() => setLeagueMenuOpen(o => !o)}
                            aria-expanded={leagueMenuOpen}
                            aria-haspopup="menu"
                            aria-controls={leagueListId}
                        >
                            Leagues
                            <svg
                                className="navbar__league-chevron"
                                width="12"
                                height="12"
                                viewBox="0 0 12 12"
                                fill="none"
                                stroke="currentColor"
                                strokeWidth="2"
                                aria-hidden="true"
                                style={{ display: "block" }}
                            >
                                <path d="M2 4l4 4 4-4" />
                            </svg>
                        </button>

                        {leagueMenuOpen && (
                            <ul
                                id={leagueListId}
                                className="navbar__league-dropdown"
                                role="menu"
                                aria-label="Leagues"
                            >
                                {(leagues ?? []).map(league => (
                                    <li key={league.id} role="none">
                                        <button
                                            type="button"
                                            role="menuitem"
                                            className="navbar__league-item"
                                            onClick={() => {
                                                navigate(`/leagues/${league.shortName}`);
                                                closeLeagueMenu();
                                            }}
                                        >
                                            <LeagueLogo shortName={league.shortName} className="navbar__league-logo" />
                                            <span className="navbar__league-short">{league.shortName}</span>
                                            <span className="navbar__league-name">{league.name}</span>
                                        </button>
                                    </li>
                                ))}
                            </ul>
                        )}
                    </div>

                    <NavLink to="/standings" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Standings
                    </NavLink>
                    <NavLink to="/teams" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Teams
                    </NavLink>
                </div>

                {/* ========== LIVE INDICATOR ========== */}
                {liveCount > 0 && (
                    <NavLink
                        to="/"
                        className="navbar__live-indicator"
                        aria-label={`${liveCount} live match${liveCount !== 1 ? "es" : ""}`}
                    >
                        <span role="status" aria-live="polite" className="navbar__live-inner">
                            <span className="navbar__live-dot" aria-hidden="true" />
                            <span className="navbar__live-count">{liveCount} LIVE</span>
                        </span>
                    </NavLink>
                )}

            </div>
        </nav>
    );
}