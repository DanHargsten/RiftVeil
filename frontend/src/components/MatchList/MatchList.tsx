import { useQuery } from "@tanstack/react-query";
import { useEffect, useId, useLayoutEffect, useMemo, useRef, useState } from "react";
import { VisibilityOffIcon, VisibilityOnIcon } from "@/components/common/Icons.tsx";
import type { SpoilerPrefs } from "@/hooks/useSpoilerPrefs.ts";
import { matchesApi, tournamentsApi, type MatchListItem } from "@/lib/api.ts";
import { MatchCard } from "./MatchCard";

type GroupedMatches = {
    date: string;
    label: string;
    isToday: boolean;
    isPast: boolean;
    matches: MatchListItem[];
};

interface MatchListProps {
    tournamentId?: number | null;
    onTournamentChange?: (tournamentId: number | null) => void;
    spoilerProps: {
        spoilers: SpoilerPrefs;
        toggleGlobal: () => void;
        revealMatch: (id: number) => void;
        hideMatch: (id: number) => void;
    };
}

export function MatchList({ tournamentId, onTournamentChange, spoilerProps }: MatchListProps) {
    const { spoilers, toggleGlobal, revealMatch, hideMatch } = spoilerProps;
    const formIds = useId();
    const searchInputId = `${formIds}-search`;
    const tournamentSelectId = `${formIds}-tournament`;
    const spoilerCheckboxId = `${formIds}-spoiler`;
    const [search, setSearch] = useState("");
    const [showJumpToToday, setShowJumpToToday] = useState(false);
    const [localTournamentId, setLocalTournamentId] = useState<number | "all">(
        tournamentId != null ? tournamentId : "all",
    );
    const selectedTournamentId = onTournamentChange
        ? (tournamentId != null ? tournamentId : "all")
        : localTournamentId;
    const todayRef = useRef<HTMLDivElement>(null);
    const hasScrolled = useRef(false);
    const prevTournamentRef = useRef(selectedTournamentId);

    useLayoutEffect(() => {
        setLocalTournamentId(tournamentId != null ? tournamentId : "all");
    }, [tournamentId]);

    const { data: tournaments } = useQuery({
        queryKey: ["tournaments"],
        queryFn: () => tournamentsApi.getAll(),
    });

    const { data: matches, isLoading, error } = useQuery({
        queryKey: ["matches", selectedTournamentId, "window"],
        queryFn: () =>
            selectedTournamentId !== "all"
                ? matchesApi.getAll({ tournamentId: selectedTournamentId })
                : (() => {
                      const from = new Date();
                      from.setDate(from.getDate() - 7);
                      const to = new Date();
                      to.setDate(to.getDate() + 7);
                      return matchesApi.getAll({ from: from.toISOString(), to: to.toISOString() });
                  })(),
    });

    const filteredMatches = useMemo(() => {
        const list = matches ?? [];
        const q = search.trim().toLowerCase();
        if (!q) return list;
        return list.filter((match) => matchSearchHaystack(match).includes(q));
    }, [matches, search]);

    const groupedMatches = useMemo(
        () => groupMatchesByDate(filteredMatches, { insertEmptyToday: filteredMatches.length > 0 }),
        [filteredMatches],
    );

    useLayoutEffect(() => {
        if (prevTournamentRef.current !== selectedTournamentId) {
            hasScrolled.current = false;
            prevTournamentRef.current = selectedTournamentId;
        }
        if (groupedMatches.length === 0 || !todayRef.current) return;
        if (hasScrolled.current) return;

        let alive = true;
        const apply = () => {
            if (!alive || !todayRef.current) return;
            todayRef.current.scrollIntoView({ block: "center", behavior: "auto" });
            hasScrolled.current = true;
        };

        const id = requestAnimationFrame(() => {
            requestAnimationFrame(apply);
        });
        return () => {
            alive = false;
            cancelAnimationFrame(id);
        };
    }, [groupedMatches, selectedTournamentId]);

    useEffect(() => {
        const todayAnchor = todayRef.current;
        const hasTodayGroup = groupedMatches.some((group) => group.isToday);
        if (!todayAnchor || !hasTodayGroup) {
            setShowJumpToToday(false);
            return;
        }

        const observer = new IntersectionObserver(
            ([entry]) => {
                setShowJumpToToday(!entry.isIntersecting);
            },
            {
                root: null,
                threshold: 0.35,
                rootMargin: "-90px 0px 0px 0px",
            },
        );

        observer.observe(todayAnchor);
        return () => observer.disconnect();
    }, [groupedMatches]);

    function jumpToToday() {
        if (!todayRef.current) return;
        todayRef.current.scrollIntoView({ block: "center", behavior: "smooth" });
    }

    if (isLoading) return <div className="match-list__state">Loading matches...</div>;
    if (error) return <div className="match-list__state match-list__state--error">Error loading matches.</div>;

    return (
        <div className="match-list">
            <div className="match-list__toolbar">
                <div className="match-list__toolbar-left">
                    <div className="match-list__search">
                        <label htmlFor={searchInputId} className="sr-only">
                            Search teams, tournaments, or region
                        </label>
                        <svg
                            className="match-list__search-icon"
                            width="16"
                            height="16"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            strokeWidth="2"
                            aria-hidden="true"
                        >
                            <circle cx="11" cy="11" r="8" />
                            <path d="m21 21-4.35-4.35" />
                        </svg>
                        <input
                            id={searchInputId}
                            className="match-list__search-input"
                            type="search"
                            placeholder="Search teams, tournaments, or region…"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            autoComplete="off"
                            spellCheck={false}
                        />
                    </div>

                    <label htmlFor={tournamentSelectId} className="sr-only">
                        Tournament
                    </label>
                    <select
                        id={tournamentSelectId}
                        className="match-list__filter-select match-list__filter-select--mobile"
                        value={selectedTournamentId}
                        onChange={(e) => {
                            const nextTournamentId = e.target.value === "all" ? "all" : Number(e.target.value);
                            if (onTournamentChange) {
                                onTournamentChange(nextTournamentId === "all" ? null : nextTournamentId);
                                return;
                            }
                            setLocalTournamentId(nextTournamentId);
                        }}
                    >
                        <option value="all">All Tournaments</option>
                        {tournaments?.map((tournament) => (
                            <option key={tournament.id} value={tournament.id}>
                                {tournament.name}
                            </option>
                        ))}
                    </select>
                </div>

                {showJumpToToday && (
                    <div className="match-list__toolbar-center">
                        <button type="button" className="match-list__jump-today" onClick={jumpToToday}>
                            Jump to today
                        </button>
                    </div>
                )}

                <div className="match-list__toolbar-right">
                    <div className="match-list__spoiler-toggle">
                        <label htmlFor={spoilerCheckboxId} className="match-list__spoiler-text">
                            <span className="match-list__spoiler-label">
                                {spoilers.globalEnabled ? <VisibilityOffIcon size={20} /> : <VisibilityOnIcon size={20} />}
                                Hide spoilers
                            </span>
                            <span className="match-list__spoiler-status">
                                {spoilers.globalEnabled
                                    ? "Scores hidden across matches"
                                    : "Scores visible across matches"}
                            </span>
                        </label>
                        <div className={`match-list__toggle-track ${spoilers.globalEnabled ? "match-list__toggle-track--on" : ""}`}>
                            <input
                                id={spoilerCheckboxId}
                                type="checkbox"
                                checked={spoilers.globalEnabled}
                                onChange={toggleGlobal}
                                className="match-list__toggle-input"
                            />
                            <div className="match-list__toggle-thumb" />
                        </div>
                    </div>
                </div>
            </div>

            {groupedMatches.length === 0 ? (
                <div className="match-list__state">No matches found.</div>
            ) : (
                <div className="match-list__groups">
                    {groupedMatches.map((group) => (
                        <div
                            key={group.date}
                            ref={group.isToday ? todayRef : undefined}
                            className={[
                                "match-list__group",
                                group.isToday ? "match-list__group--today" : "",
                                group.isPast ? "match-list__group--past" : "",
                                group.isToday && group.matches.length === 0 ? "match-list__group--empty-today" : "",
                            ]
                                .filter(Boolean)
                                .join(" ")}
                        >
                            <h3 className="match-list__group-title">{group.label}</h3>
                            {group.matches.length > 0 ? (
                                <div className="match-list__items">
                                    {group.matches.map((match) => (
                                        <MatchCard
                                            key={match.id}
                                            match={match}
                                            spoilers={spoilers}
                                            onReveal={() => revealMatch(match.id)}
                                            onHide={() => hideMatch(match.id)}
                                        />
                                    ))}
                                </div>
                            ) : group.isToday ? (
                                <p className="match-list__no-matches-today">No matches today</p>
                            ) : null}
                        </div>
                    ))}
                </div>
            )}

        </div>
    );
}

function matchSearchHaystack(match: MatchListItem): string {
    const parts = [
        match.team1Name,
        match.team2Name,
        match.team1ShortName,
        match.team2ShortName,
        match.tournamentName,
        match.tournamentStage,
        match.leagueName,
        match.leagueShortName,
        match.leagueRegion,
        match.round,
    ];
    return parts.filter((p): p is string => typeof p === "string" && p.length > 0).join(" ").toLowerCase();
}

function pad2(n: number): string {
    return String(n).padStart(2, "0");
}

function localDateKey(d: Date): string {
    return `${d.getFullYear()}-${pad2(d.getMonth() + 1)}-${pad2(d.getDate())}`;
}

function localTodayStart(): Date {
    const t = new Date();
    t.setHours(0, 0, 0, 0);
    return t;
}

function parseDateKeyLocal(dateKey: string): Date {
    const [y, mo, da] = dateKey.split("-").map(Number);
    return new Date(y, mo - 1, da);
}

function groupMatchesByDate(
    matches: MatchListItem[],
    opts: { insertEmptyToday: boolean },
): GroupedMatches[] {
    const todayStart = localTodayStart();
    const todayKey = localDateKey(todayStart);
    const grouped = new Map<string, MatchListItem[]>();

    for (const match of matches) {
        const dateKey = localDateKey(new Date(match.startsAtUtc));
        if (!grouped.has(dateKey)) grouped.set(dateKey, []);
        grouped.get(dateKey)!.push(match);
    }

    let groups: GroupedMatches[] = Array.from(grouped.keys())
        .sort()
        .map((dateKey) => {
            const matchLocalDate = parseDateKeyLocal(dateKey);
            const matchesForDay = grouped.get(dateKey)!.sort(
                (a, b) => new Date(a.startsAtUtc).getTime() - new Date(b.startsAtUtc).getTime(),
            );
            const isToday = dateKey === todayKey;
            const isPast = dateKey < todayKey;
            return {
                date: dateKey,
                label: formatDateLabel(matchLocalDate, todayStart),
                isToday,
                isPast,
                matches: matchesForDay,
            };
        });

    if (opts.insertEmptyToday && !groups.some((g) => g.date === todayKey)) {
        const todayGroup: GroupedMatches = {
            date: todayKey,
            label: "Today",
            isToday: true,
            isPast: false,
            matches: [],
        };
        const insertAt = groups.findIndex((g) => g.date > todayKey);
        if (insertAt === -1) groups = [...groups, todayGroup];
        else groups = [...groups.slice(0, insertAt), todayGroup, ...groups.slice(insertAt)];
    }

    return groups;
}

function formatDateLabel(matchLocalDate: Date, todayStart: Date): string {
    const norm = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
    const diff = norm(matchLocalDate) - norm(todayStart);
    if (diff === 0) return "Today";
    if (diff === -86400000) return "Yesterday";
    if (diff === 86400000) return "Tomorrow";
    const weekday = matchLocalDate.toLocaleDateString("en-US", { weekday: "long" });
    const datePart = matchLocalDate.toLocaleDateString("en-US", { month: "short", day: "numeric" });
    return `${weekday}, ${datePart}`;
}
