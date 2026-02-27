import { matchesApi, type MatchListItem } from "@/lib/api.ts";
import { useQuery } from "@tanstack/react-query";
import { useState, useEffect, useRef } from "react";
import { MatchCard } from "./MatchCard";

type SpoilerPrefs = {
    globalEnabled: boolean;
    revealedMatchIds: Set<number>;
};

type GroupedMatches = {
    date: string;
    label: string;
    isToday: boolean;
    isPast: boolean;
    matches: MatchListItem[];
};

interface MatchListProps {
    tournamentId?: number | null;
}

/** Lists matches grouped by date, with date-range loading and a spoiler toggle. */
export function MatchList({ tournamentId }: MatchListProps) {
    const [spoilers, setSpoilers] = useState<SpoilerPrefs>({
        globalEnabled: false,
        revealedMatchIds: new Set<number>(),
    });

    const todayRef = useRef<HTMLDivElement>(null);
    const hasScrolled = useRef(false);

    // Determine if we're in tournament mode (show all) or date mode
    const isTournamentMode = tournamentId != null;
    
    const {
        data: matches,
        isLoading,
        error,
    } = useQuery({
        queryKey: ["matches", tournamentId],
        queryFn: () => isTournamentMode
            ? matchesApi.getAll({ tournamentId: tournamentId! })
            : matchesApi.getRecent(15),
    });

    // Auto-scroll to "Today" on the first load (not on later re-renders)
    useEffect(() => {
        if (matches && todayRef.current && !hasScrolled.current) {
            hasScrolled.current = true;
            setTimeout(() => {
                todayRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
            }, 100);
        }
    }, [matches]);

    // Reset scroll tracking when switching tournament
    useEffect(() => {
        hasScrolled.current = false;
    }, [tournamentId]);
    
    const toggleGlobal = () => {
        setSpoilers((prev) => ({
            ...prev,
            globalEnabled: !prev.globalEnabled,
        }));
    };

    const revealMatch = (matchId: number) => {
        setSpoilers((prev) => {
            const next = new Set(prev.revealedMatchIds);
            next.add(matchId);
            return { ...prev, revealedMatchIds: next };
        });
    };

    const hideMatch = (matchId: number) => {
        setSpoilers((prev) => {
            const next = new Set(prev.revealedMatchIds);
            next.delete(matchId);
            return { ...prev, revealedMatchIds: next };
        });
    };

    if (isLoading) {
        return (
            <div className="match-list__state match-list__state--loading">
                Loading matches...
            </div>
        );
    }

    if (error) {
        return (
            <div className="match-list__state match-list__state--error">
                Error loading matches: {error.message}
            </div>
        );
    }

    if (!matches || matches.length === 0) {
        return (
            <div className="match-list__state match-list__state--empty">
                No matches found.
            </div>
        );
    }

    const groupedMatches = groupMatchesByDate(matches);

    return (
        <div className="match-list">
            <div className="match-list__header">
                <h2 className="match-list__title">
                    {isTournamentMode ? "Tournament matches" : "Latest matches"}
                </h2>

                <label className="match-list__spoiler-toggle">
                    <input
                        type="checkbox"
                        checked={spoilers.globalEnabled}
                        onChange={toggleGlobal}
                    />
                    <span>Show spoilers</span>
                </label>
            </div>
            
            <div className="match-list__groups">
                {groupedMatches.map((group) => (
                    <div
                        key={group.date}
                        className={`match-list__group ${group.isToday ? "match-list__group--today" : ""} ${group.isPast ? "match-list__group--past" : ""}`}
                        ref={group.isToday ? todayRef : null}
                    >
                        <h3 className="match-list__group-title">{group.label}</h3>

                        {group.matches.length === 0 ? (
                            <div className="match-list__group-empty">
                                No matches scheduled
                            </div>
                        ) : (
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
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}

/** Groups matches by date and sorts within each day. */
function groupMatchesByDate(matches: MatchListItem[]): GroupedMatches[] {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const grouped = new Map<string, MatchListItem[]>();

    matches.forEach((match) => {
        const matchDate = new Date(match.startsAtUtc);
        const dateKey = matchDate.toISOString().split("T")[0];

        if (!grouped.has(dateKey)) {
            grouped.set(dateKey, []);
        }
        grouped.get(dateKey)!.push(match);
    });

    const sortedDates = Array.from(grouped.keys()).sort();

    return sortedDates.map((dateKey) => {
        const matchDate = new Date(dateKey);
        const matchesForDay = grouped.get(dateKey)!;

        matchesForDay.sort(
            (a, b) =>
                new Date(a.startsAtUtc).getTime() - new Date(b.startsAtUtc).getTime()
        );

        const isToday = matchDate.getTime() === today.getTime();
        const isPast = matchDate < today;

        return {
            date: dateKey,
            label: formatDateLabel(matchDate, today),
            isToday,
            isPast,
            matches: matchesForDay,
        };
    });
}

/** Returns "Today", "Yesterday", "Tomorrow", or a date like "Måndag 17/2". */
function formatDateLabel(date: Date, today: Date): string {
    const normalize = (d: Date) =>
        new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();

    const normDate = normalize(date);
    const normToday = normalize(today);
    const normYesterday = normalize(new Date(today.getTime() - 86400000));
    const normTomorrow = normalize(new Date(today.getTime() + 86400000));

    if (normDate === normToday) {
        return "Today";
    } else if (normDate === normYesterday) {
        return "Yesterday";
    } else if (normDate === normTomorrow) {
        return "Tomorrow";
    } else {
        const weekday = date.toLocaleDateString("sv-SE", { weekday: "long" });
        const day = date.getDate();
        const month = date.getMonth() + 1;
        const capWeekday = weekday.charAt(0).toUpperCase() + weekday.slice(1);
        return `${capWeekday} ${day}/${month}`;
    }
}