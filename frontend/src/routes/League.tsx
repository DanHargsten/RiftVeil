import { useQuery } from "@tanstack/react-query";
import { useId, useState } from "react";
import { useParams } from "react-router-dom";
import { LeagueLogo } from "@/components/common/Logos.tsx";
import { MatchCard } from "@/components/MatchList/MatchCard.tsx";
import { useSpoilerPrefs } from "@/hooks/useSpoilerPrefs.ts";
import { leaguesApi, matchesApi, type MatchListItem } from "@/lib/api.ts";

export function League() {
    const { shortName } = useParams<{ shortName: string }>();
    const ids = useId();
    const tournamentFieldId = `${ids}-tournament`;
    const { spoilers, revealMatch, hideMatch } = useSpoilerPrefs();

    const { data: leagues } = useQuery({
        queryKey: ["leagues"],
        queryFn: () => leaguesApi.getAll(),
    });

    const league = leagues?.find(
        leagueItem => leagueItem.shortName === shortName?.toUpperCase(),
    );

    const { data: leagueDetails } = useQuery({
        queryKey: ["league", league?.id],
        queryFn: () => leaguesApi.getById(league!.id),
        enabled: !!league?.id,
    });

    const tournaments = leagueDetails?.tournaments
        ?.slice()
        .sort(
            (left, right) =>
                new Date(right.startsAtUtc).getTime() - new Date(left.startsAtUtc).getTime(),
        );

    const defaultId =
        tournaments?.find(tournament => tournament.status === "Ongoing")?.id
        ?? tournaments?.[0]?.id
        ?? null;
    const [selectedTournamentId, setSelectedTournamentId] = useState<number | null>(null);
    const activeTournamentId = selectedTournamentId ?? defaultId;
    // null = never toggled (use default), string = user-selected round, "CLOSED" = explicitly collapsed
    const [openRound, setOpenRound] = useState<string | null>(null);

    const { data: matches, isLoading: matchesLoading } = useQuery({
        queryKey: ["matches", "tournament", activeTournamentId],
        queryFn: () => matchesApi.getAll({ tournamentId: activeTournamentId! }),
        enabled: !!activeTournamentId,
    });

    const isLoading = !leagues || !leagueDetails;

    if (leagues && !league) {
        return (
            <div className="league-page__state" role="status" aria-live="polite">
                League not found.
            </div>
        );
    }

    const roundGroups = groupByRound(matches ?? []);

    const now = new Date();
    const selectedTournament = tournaments?.find(
        tournament => tournament.id === activeTournamentId,
    );
    const activeRoundKey = findActiveRound(roundGroups, now);

    // Only auto-open the round that is currently in progress; finished/upcoming tournaments stay collapsed
    const defaultOpenRound =
        selectedTournament?.status === "Ongoing" ? activeRoundKey : null;

    const activeRound = openRound === "CLOSED" ? null : (openRound ?? defaultOpenRound);

    function toggleRound(round: string) {
        if (activeRound === round) {
            // Collapsing the default round requires an explicit "closed" state
            setOpenRound("CLOSED");
        } else {
            setOpenRound(round);
        }
    }

    return (
        <div className="league-page">
            {/* ========== HEADER ========== */}
            <header className="league-page__header container">
                {league && <LeagueLogo shortName={league.shortName} className="league-page__logo" />}
                <div className="league-page__header-text">
                    <h1 className="league-page__title">{league?.shortName ?? shortName}</h1>
                    <span className="league-page__subtitle">{league?.name}</span>
                </div>

                {tournaments && tournaments.length > 0 && (
                    <div className="league-page__tournament-field">
                        <label htmlFor={tournamentFieldId} className="league-page__tournament-label">
                            Tournament
                        </label>
                        <select
                            id={tournamentFieldId}
                            className="league-page__tournament-select"
                            value={activeTournamentId ?? ""}
                            onChange={event => {
                                setSelectedTournamentId(Number(event.target.value));
                                setOpenRound(null);
                            }}
                        >
                            {tournaments.map(tournament => (
                                <option key={tournament.id} value={tournament.id}>
                                    {tournament.name}
                                </option>
                            ))}
                        </select>
                    </div>
                )}
            </header>

            {/* ========== MAIN ========== */}
            <div className="league-page__content container">
                {(isLoading || matchesLoading) && (
                    <div className="league-page__state" role="status" aria-live="polite">
                        Loading...
                    </div>
                )}
                {!isLoading && !matchesLoading && roundGroups.length === 0 && (
                    <div className="league-page__state" role="status" aria-live="polite">
                        No matches found.
                    </div>
                )}
                {roundGroups.map(group => {
                    const isOpen = activeRound === group.round;
                    const roundStatus = getRoundStatus(group.matches, now);
                    const isCurrentRound = group.round === activeRoundKey;
                    const dayGroups = groupByDay(group.matches);
                    const roundPanelId = `league-round-panel-${group.round.replace(/[^a-zA-Z0-9_-]/g, "_")}`;

                    return (
                        <div
                            key={group.round}
                            className={`league-page__round ${roundStatus === "done" ? "league-page__round--finished" : ""} ${roundStatus === "upcoming" ? "league-page__round--upcoming" : ""}`}
                        >
                            <h2 className="league-page__round-heading">
                                <button
                                    type="button"
                                    className={`league-page__round-header ${isCurrentRound ? "league-page__round-header--current" : ""}`}
                                    onClick={() => toggleRound(group.round)}
                                    aria-expanded={isOpen}
                                    aria-controls={roundPanelId}
                                >
                                    <span className="league-page__round-title">{group.round}</span>
                                    <span className="league-page__round-dates">
                                        {getRoundDateRange(group.matches)}
                                    </span>
                                    {roundStatus ? (
                                        <span
                                            className={`league-page__round-badge league-page__round-badge--${roundStatus}`}
                                        >
                                            {roundStatus === "done"
                                                ? "Done"
                                                : roundStatus === "upcoming"
                                                    ? "Upcoming"
                                                    : "Current"}
                                        </span>
                                    ) : null}
                                    <svg
                                        className={`league-page__round-chevron ${isOpen ? "league-page__round-chevron--open" : ""}`}
                                        width="16"
                                        height="16"
                                        viewBox="0 0 16 16"
                                        fill="none"
                                        stroke="currentColor"
                                        strokeWidth="2"
                                        aria-hidden
                                        focusable="false"
                                    >
                                        <path d="M4 6l4 4 4-4" />
                                    </svg>
                                </button>
                            </h2>

                            {isOpen && (
                                <div
                                    className="league-page__round-body"
                                    id={roundPanelId}
                                    role="region"
                                    aria-label={group.round}
                                >
                                    {dayGroups.map(day => {
                                        const dayHeadingId = `league-day-${day.dateKey}`;
                                        return (
                                            <section
                                                key={day.dateKey}
                                                className="league-page__day"
                                                aria-labelledby={dayHeadingId}
                                            >
                                                <div className="league-page__day-header">
                                                    <span id={dayHeadingId} className="league-page__day-label">
                                                        {day.label}
                                                    </span>
                                                    {day.isToday && (
                                                        <span className="league-page__day-today">
                                                            Today · {day.matches.length}{" "}
                                                            {day.matches.length === 1 ? "match" : "matches"}
                                                        </span>
                                                    )}
                                                </div>
                                                <div className="league-page__items">
                                                    {day.matches.map(match => (
                                                        <MatchCard
                                                            key={match.id}
                                                            match={match}
                                                            spoilers={spoilers}
                                                            onReveal={() => revealMatch(match.id)}
                                                            onHide={() => hideMatch(match.id)}
                                                        />
                                                    ))}
                                                </div>
                                            </section>
                                        );
                                    })}
                                </div>
                            )}
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

function findActiveRound(
    roundGroups: { round: string; matches: MatchListItem[] }[],
    now: Date,
): string | null {
    for (let index = roundGroups.length - 1; index >= 0; index -= 1) {
        if (isRoundActive(roundGroups[index].matches, now)) {
            return roundGroups[index].round;
        }
    }
    return null;
}

function isRoundActive(matches: MatchListItem[], now: Date): boolean {
    if (matches.some(match => match.status === "Live")) return true;

    const allFinished = matches.every(match => match.status === "Finished");
    const allUpcoming = matches.every(match => match.status === "Scheduled");
    if (allFinished || allUpcoming) return false;

    const hasStarted = matches.some(
        match => match.status === "Finished" || new Date(match.startsAtUtc) <= now,
    );
    const hasRemaining = matches.some(
        match => match.status === "Scheduled" || match.status === "Live",
    );
    return hasStarted && hasRemaining;
}

type RoundStatus = "done" | "upcoming" | "current";

function getRoundStatus(matches: MatchListItem[], now: Date): RoundStatus | null {
    if (matches.every(match => match.status === "Finished")) return "done";
    if (matches.every(match => match.status === "Scheduled")) return "upcoming";
    if (isRoundActive(matches, now)) return "current";
    return null;
}

function groupByRound(matches: MatchListItem[]) {
    const map = new Map<string, MatchListItem[]>();
    matches.forEach(match => {
        const key = match.round ?? "Other";
        if (!map.has(key)) map.set(key, []);
        map.get(key)!.push(match);
    });

    const sorted = Array.from(map.keys()).sort((roundKeyA, roundKeyB) => {
        const numA = parseInt(roundKeyA.replace(/\D/g, "")) || 0;
        const numB = parseInt(roundKeyB.replace(/\D/g, "")) || 0;
        return numA - numB;
    });

    return sorted.map(round => ({
        round,
        matches: map.get(round)!.sort(
            (left, right) =>
                new Date(left.startsAtUtc).getTime() - new Date(right.startsAtUtc).getTime(),
        ),
    }));
}

function groupByDay(matches: MatchListItem[]) {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const map = new Map<string, MatchListItem[]>();

    matches.forEach(match => {
        const key = new Date(match.startsAtUtc).toISOString().split("T")[0];
        if (!map.has(key)) map.set(key, []);
        map.get(key)!.push(match);
    });

    return Array.from(map.keys()).sort().map(dateKey => {
        const calendarDate = new Date(dateKey);
        const isToday = calendarDate.getTime() === today.getTime();
        const label = calendarDate.toLocaleDateString("en-GB", {
            weekday: "long",
            day: "numeric",
            month: "short",
        });
        return {
            dateKey,
            label: label.charAt(0).toUpperCase() + label.slice(1),
            isToday,
            matches: map.get(dateKey)!,
        };
    });
}

function getRoundDateRange(matches: MatchListItem[]): string {
    if (matches.length === 0) return "";
    const matchStartDates = matches.map((match) => new Date(match.startsAtUtc));
    const earliestStart = new Date(
        Math.min(...matchStartDates.map((start) => start.getTime())),
    );
    const latestStart = new Date(
        Math.max(...matchStartDates.map((start) => start.getTime())),
    );
    const formatDate = (date: Date, includeYear: boolean) =>
        date.toLocaleDateString("en-GB", {
            day: "numeric",
            month: "short",
            ...(includeYear ? { year: "numeric" } : {}),
        });

    const sameDay = earliestStart.toDateString() === latestStart.toDateString();
    const sameYear = earliestStart.getFullYear() === latestStart.getFullYear();

    if (sameDay) return formatDate(earliestStart, true);
    if (sameYear) return `${formatDate(earliestStart, false)} – ${formatDate(latestStart, true)}`;
    return `${formatDate(earliestStart, true)} – ${formatDate(latestStart, true)}`;
}
