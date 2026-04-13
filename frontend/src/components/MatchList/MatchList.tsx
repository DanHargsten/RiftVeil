import { matchesApi, tournamentsApi, type MatchListItem } from "@/lib/api.ts";
import { useQuery } from "@tanstack/react-query";
import { useEffect, useRef, useState } from "react";
import { MatchCard } from "./MatchCard";
import type { SpoilerPrefs } from "@/hooks/useSpoilerPrefs.ts";
import { VisibilityOffIcon, VisibilityOnIcon } from "@/components/common/Icons.tsx";


type GroupedMatches = {
    date: string;
    label: string;
    isToday: boolean;
    isPast: boolean;
    matches: MatchListItem[];
};

interface MatchListProps {
    tournamentId?: number | null;
    spoilerProps: {
        spoilers: SpoilerPrefs;
        toggleGlobal: () => void;
        revealMatch: (id: number) => void;
        hideMatch: (id: number) => void;
    };
}

export function MatchList({ spoilerProps }: MatchListProps) {
    const { spoilers, toggleGlobal, revealMatch, hideMatch } = spoilerProps;
    const [search, setSearch] = useState("");
    const [selectedTournamentId, setSelectedTournamentId] = useState<number | "all">("all");
    const todayRef = useRef<HTMLDivElement>(null);
    const hasScrolled = useRef(false);

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

    useEffect(() => {
        if (matches && todayRef.current && !hasScrolled.current) {
            hasScrolled.current = true;
            setTimeout(() => {
                todayRef.current?.scrollIntoView({ behavior: "smooth", block: "center" });
            }, 100);
        }
    }, [matches]);

    useEffect(() => {
        hasScrolled.current = false;
    }, [selectedTournamentId]);

    const filteredMatches = (matches ?? []).filter((m) => {
        if (!search.trim()) return true;
        const q = search.toLowerCase();
        return (
            m.team1Name.toLowerCase().includes(q) ||
            m.team2Name.toLowerCase().includes(q) ||
            m.team1ShortName.toLowerCase().includes(q) ||
            m.team2ShortName.toLowerCase().includes(q)
        );
    });

    if (isLoading) return <div className="match-list__state">Loading matches...</div>;
    if (error) return <div className="match-list__state match-list__state--error">Error loading matches.</div>;

    const groupedMatches = groupMatchesByDate(filteredMatches);

    return (
        <div className="match-list">

            {/* Toolbar */}
            <div className="match-list__toolbar">
                <div className="match-list__toolbar-left">
                    <div className="match-list__search">
                        <svg className="match-list__search-icon" width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <circle cx="11" cy="11" r="8"/><path d="m21 21-4.35-4.35"/>
                        </svg>
                        <input
                            className="match-list__search-input"
                            type="text"
                            placeholder="Search teams..."
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                        />
                    </div>

                    <select
                        className="match-list__filter-select"
                        value={selectedTournamentId}
                        onChange={(e) =>
                            setSelectedTournamentId(
                                e.target.value === "all" ? "all" : Number(e.target.value)
                            )
                        }
                    >
                        <option value="all">All Tournaments</option>
                        {tournaments?.map((t) => (
                            <option key={t.id} value={t.id}>
                                {t.name}
                            </option>
                        ))}
                    </select>
                </div>

                <label className="match-list__spoiler-toggle">
                    <div className="match-list__spoiler-text">
                        <span className="match-list__spoiler-label">
                            {spoilers.globalEnabled
                                ? <VisibilityOffIcon size={20} />
                                : <VisibilityOnIcon size={20} />
                            }
                            Hide spoilers
                        </span>
                        <span className="match-list__spoiler-status">
                            {spoilers.globalEnabled ? "Scores hidden across matches" : "Scores visible across matches"}
                        </span>                        
                    </div>
                    <div className={`match-list__toggle-track ${spoilers.globalEnabled ? "match-list__toggle-track--on" : ""}`}>
                        <input
                            type="checkbox"
                            checked={spoilers.globalEnabled}
                            onChange={toggleGlobal}
                            className="match-list__toggle-input"
                        />
                        <div className="match-list__toggle-thumb" />
                    </div>
                </label>
            </div>

            {/* Match-grupper */}
            {groupedMatches.length === 0 ? (
                <div className="match-list__state">No matches found.</div>
            ) : (
                <div className="match-list__groups">
                    {groupedMatches.map((group) => (
                        <div
                            key={group.date}
                            className={`match-list__group ${group.isToday ? "match-list__group--today" : ""} ${group.isPast ? "match-list__group--past" : ""}`}
                            ref={group.isToday || (!groupedMatches.some(g => g.isToday) && group === groupedMatches.find(g => !g.isPast)) ? todayRef : null}
                        >
                            <h3 className="match-list__group-title">{group.label}</h3>
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
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

function groupMatchesByDate(matches: MatchListItem[]): GroupedMatches[] {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const grouped = new Map<string, MatchListItem[]>();

    matches.forEach((match) => {
        const dateKey = new Date(match.startsAtUtc).toISOString().split("T")[0];
        if (!grouped.has(dateKey)) grouped.set(dateKey, []);
        grouped.get(dateKey)!.push(match);
    });

    return Array.from(grouped.keys())
        .sort()
        .map((dateKey) => {
            const matchDate = new Date(dateKey);
            const matchesForDay = grouped.get(dateKey)!.sort(
                (a, b) => new Date(a.startsAtUtc).getTime() - new Date(b.startsAtUtc).getTime()
            );
            const isToday = matchDate.getTime() === today.getTime();
            return {
                date: dateKey,
                label: formatDateLabel(matchDate, today),
                isToday,
                isPast: matchDate < today,
                matches: matchesForDay,
            };
        });
}

function formatDateLabel(date: Date, today: Date): string {
    const norm = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
    const diff = norm(date) - norm(today);
    if (diff === 0) return "Today";
    if (diff === -86400000) return "Yesterday";
    if (diff === 86400000) return "Tomorrow";
    const weekday = date.toLocaleDateString("sv-SE", { weekday: "long" });
    return `${weekday.charAt(0).toUpperCase() + weekday.slice(1)} ${date.getDate()}/${date.getMonth() + 1}`;
}