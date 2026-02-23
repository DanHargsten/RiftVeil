import { matchesApi, type MatchListItem } from "@/lib/api.ts";
import { useQuery } from "@tanstack/react-query";
import { useState, useEffect, useRef, useCallback } from "react";
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

/** How many days to load in each direction from today. */
const INITIAL_DAYS = 7;
/** How many days to load when clicking "Load earlier". */
const LOAD_MORE_DAYS = 7;

/** Lists matches grouped by date with date-based loading and spoiler toggle. */
export function MatchList({ tournamentId }: MatchListProps) {
    const [spoilers, setSpoilers] = useState<SpoilerPrefs>({
        globalEnabled: false,
        revealedMatchIds: new Set<number>(),
    });

    // How many days back we're currently showing
    const [daysBefore, setDaysBefore] = useState(INITIAL_DAYS);

    const todayRef = useRef<HTMLDivElement>(null);
    const hasScrolled = useRef(false);

    // Determine if we're in tournament mode (show all) or date mode
    const isTournamentMode = tournamentId != null;

    // Build query params
    const queryParams = (() => {
        if (isTournamentMode) {
            return { tournamentId: tournamentId! };
        }
        const now = new Date();
        const from = new Date(now);
        from.setDate(from.getDate() - daysBefore);
        from.setHours(0, 0, 0, 0);

        const to = new Date(now);
        to.setDate(to.getDate() + INITIAL_DAYS);
        to.setHours(23, 59, 59, 999);

        return { from: from.toISOString(), to: to.toISOString() };
    })();

    const {
        data: matches,
        isLoading,
        error,
        isFetching,
    } = useQuery({
        queryKey: ["matches", tournamentId, isTournamentMode ? null : daysBefore],
        queryFn: () => matchesApi.getAll(queryParams),
    });

    // Auto-scroll to "Today" on initial load (not on subsequent re-renders)
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
        setDaysBefore(INITIAL_DAYS);
    }, [tournamentId]);

    const loadEarlier = useCallback(() => {
        setDaysBefore(prev => prev + LOAD_MORE_DAYS);
    }, []);

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

            {/* Load earlier button (only in date mode) */}
            {!isTournamentMode && (
                <button
                    className="match-list__load-more"
                    onClick={loadEarlier}
                    disabled={isFetching}
                >
                    {isFetching ? "Loading..." : "Load earlier matches"}
                </button>
            )}

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

/** Returns "Today", "Yesterday", "Tomorrow", or "Måndag 17/2" style date. */
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