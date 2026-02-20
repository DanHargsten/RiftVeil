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
    // Filter on a specific tournament. null = show latest matches (±7 days).
    tournamentId: number | null;
}

// Lists matches grouped by date with spoiler toggle
export function MatchList({ tournamentId }: MatchListProps) {
    const [spoilers, setSpoilers] = useState<SpoilerPrefs>({
        globalEnabled: false,
        revealedMatchIds: new Set<number>(),
    });

    const todayRef = useRef<HTMLDivElement>(null);
    const hasScrolled = useRef(false);

    const {
        data: allMatches,
        isLoading,
        error,
    } = useQuery({
        queryKey: ["matches", tournamentId ?? "all"],
        queryFn: () => matchesApi.getAll(tournamentId ?? undefined),
    });

    // Filter client page: without tournament filter, only show ±7 days around today
    const matches = allMatches ? filterByTimeWindow(allMatches, tournamentId) : undefined;

    // Auto-scroll to "Today" only on first load
    useEffect(() => {
        if (matches && todayRef.current && !hasScrolled.current) {
            hasScrolled.current = true;
            setTimeout(() => {
                todayRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
            }, 100);
        }
    }, [matches]);

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
                Could not load matches: {error.message}
            </div>
        );
    }

    if (!matches || matches.length === 0) {
        return (
            <div className="match-list__state match-list__state--empty">
                {tournamentId
                    ? "No matches found for this tournament."
                    : "No matches today."}
            </div>
        );
    }

    const groupedMatches = groupMatchesByDate(matches);

    return (
        <div className="match-list">
            <div className="match-list__header">
                <h2 className="match-list__title">
                    {tournamentId ? matches[0]?.tournamentName ?? "Matcher" : "Latest matches"}
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
                                No matcher scheduled
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

// Without tournament filter: only show matches ±7 days around today.
function filterByTimeWindow(matches: MatchListItem[], tournamentId: number | null): MatchListItem[] {
    if (tournamentId) return matches; // Show all by tournament filter

    const now = new Date();
    const pastCutoff = new Date(now);
    pastCutoff.setDate(pastCutoff.getDate() - 7);

    const futureCutoff = new Date(now);
    futureCutoff.setDate(futureCutoff.getDate() + 7);

    return matches.filter(m => {
        const date = new Date(m.startsAtUtc);
        return date >= pastCutoff && date <= futureCutoff;
    });
}

// Groups matches by date, sorted chronologically.
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
        const matchDate = new Date(dateKey + "T00:00:00");
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

// Returns "Today", "Yesterday", "Tomorrow", or formatted date.
function formatDateLabel(date: Date, today: Date): string {
    // Normalize to midnight so the comparison always works (regardless of time of day)
    const normalize = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime().toString();

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
        // Fetch the parts separately for full control over capitalization
        const weekday = date.toLocaleDateString("sv-SE", { weekday: "long" });
        const day = date.getDate();
        let month = date.toLocaleDateString("sv-SE", { month: "short" });
        
        // Remove potential period after month ("feb." -> "feb"
        month = month.replace(/\.$/, "");
        
        // Capitalize the first letter of both the day of the week and the month
        const capWeekday = weekday.charAt(0).toUpperCase() + weekday.slice(1);
        const capMonth = month.charAt(0).toUpperCase() + month.slice(1);
        
        return `${capWeekday} ${day} ${capMonth}`;
    }
}