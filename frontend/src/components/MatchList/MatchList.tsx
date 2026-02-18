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

/** Lists matches grouped by date with global spoiler toggle. */
export function MatchList() {
    const [spoilers, setSpoilers] = useState<SpoilerPrefs>({
        globalEnabled: false,
        revealedMatchIds: new Set<number>(),
    });

    const todayRef = useRef<HTMLDivElement>(null);

    const {
        data: matches,
        isLoading,
        error,
    } = useQuery({
        queryKey: ["matches", "all"],
        queryFn: () => matchesApi.getAll(),
    });

    // Auto-scroll to "Today" section on mount
    useEffect(() => {
        if (matches && todayRef.current) {
            todayRef.current.scrollIntoView({ behavior: "smooth", block: "start" });
        }
    }, [matches]);

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
                <h2 className="match-list__title">Matches</h2>

                <label className="match-list__spoiler-toggle">
                    <input
                        type="checkbox"
                        checked={spoilers.globalEnabled}
                        onChange={toggleGlobal}
                    />
                    <span>Show spoilers globally</span>
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

/** Returns "Today", "Yesterday", "Tomorrow", or formatted date. */
function formatDateLabel(date: Date, today: Date): string {
    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    if (date.getTime() === today.getTime()) {
        return "Today";
    } else if (date.getTime() === yesterday.getTime()) {
        return "Yesterday";
    } else if (date.getTime() === tomorrow.getTime()) {
        return "Tomorrow";
    } else if (date < today) {
        return date.toLocaleDateString("en-US", {
            weekday: "long",
            month: "short",
            day: "numeric",
        });
    } else {
        return date.toLocaleDateString("en-US", {
            weekday: "long",
            month: "short",
            day: "numeric",
        });
    }
}