import { type MatchListItem } from "@/lib/api.ts";
import React, { useState } from "react";
import {ArrowDropdownIcon, PlayIcon, VisibilityOffIcon} from "@/components/common/Icons.tsx";
import {Link} from "react-router-dom";

type SpoilerPrefs = {
    globalEnabled: boolean;
    revealedMatchIds: Set<number>;
};

interface MatchCardProps {
    match: MatchListItem;
    spoilers: SpoilerPrefs;
    onReveal: () => void;
    onHide: () => void;
}


/**
 * Displays individual match information with spoiler protection.
 * Three-stage expansion: compact → score revealed → full details.
 */
export function MatchCard({ match, spoilers, onReveal, onHide }: MatchCardProps) {
    const [expanded, setExpanded] = useState(false);
    const [revealedGames, setRevealedGames] = useState<Set<number>>(new Set());

    const isFinished = match.status === "Finished";
    const isLive = match.status === "Live";

    // Global OR per-match reveal
    const showingSpoilers =
        spoilers.globalEnabled || spoilers.revealedMatchIds.has(match.id);

    const canShowScore =
        showingSpoilers &&
        isFinished &&
        match.team1Score != null &&
        match.team2Score != null;

    // Check if all played games have been individually revealed
    const allPlayedRevealed = match.games
        .filter(g => g.winningTeam != null)
        .every(g => revealedGames.has(g.gameNumber));

    // Dynamic time display: today = "14:00", this week = "2d ago", older = "4 nov"
    const getTimeDisplay = () => {
        if (isFinished) {
            const now = new Date();
            const matchDate = new Date(match.startsAtUtc);
            const diffMs = new Date(now.toDateString()).getTime() - new Date(matchDate.toDateString()).getTime();
            const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

            if (diffDays === 0) return formatTime(match.startsAtUtc);
            if (diffDays < 7) return `${diffDays}d ago`;

            return new Date(match.startsAtUtc).toLocaleDateString(undefined, {
                month: "short",
                day: "numeric"
            });
        }

        return formatTime(match.startsAtUtc);
    };

    const handleEyeClick = (e: React.MouseEvent) => {
        e.stopPropagation();
        if (showingSpoilers) {
            onHide();
        } else {
            onReveal();
        }
    };

    const toggleGameSpoiler = (gameNumber: number) => {
        setRevealedGames(prev => {
            const next = new Set(prev);
            if (next.has(gameNumber)) {
                next.delete(gameNumber);
            } else {
                next.add(gameNumber);
            }
            return next;
        });
    };

    const getStatusClass = () => {
        if (match.status === "Live") return "match-card--live";
        if (match.status === "Finished") return "match-card--finished";
        if (match.status === "Cancelled") return "match-card--cancelled";
        return "match-card--upcoming";
    };


    return (
        <article className={`match-card ${getStatusClass()} ${expanded ? "match-card--expanded" : ""}`}>

            {/* Header: League logo + Tournament + Status */}
            <div className="match-card__header">
                <div className="match-card__tournament">
                    <img
                        src={`/logos/leagues/${match.leagueShortName.toLowerCase()}.png`}
                        alt={match.leagueShortName}
                        className="match-card__league-logo"
                        onError={(e) => {
                            e.currentTarget.src = `/logos/leagues/placeholder.png`;
                        }}
                    />
                    <span className="match-card__tournament-league">{match.leagueShortName}</span>
                    {match.tournamentStage && (
                        <>
                            <span className="match-card__tournament-separator">/</span>
                            <span className="match-card__tournament-stage">{match.tournamentStage}</span>
                        </>
                    )}
                </div>
                <div className="match-card__status">
                    {isLive ? (
                        <span className="match-card__status-badge match-card__status-badge--live">LIVE</span>
                    ) : (
                        <span className="match-card__best-of">Bo{match.bestOf}</span>
                    )}
                </div>
            </div>

            {/* Main: Time + Teams */}
            <div className="match-card__main">

                <time className={`match-card__time ${isFinished ? "match-card__time--hidden" : ""}`} dateTime={match.startsAtUtc} aria-hidden={isFinished}>
                    {isFinished ? "\u00A0" : getTimeDisplay()}
                </time>

                <div className="match-card__teams">
                    {/* Team 1 */}
                    <div className="match-card__team">
                        <div className="match-card__team-logo">
                            <img
                                src={`/logos/teams/${match.team1ShortName.toLowerCase()}.png`}
                                alt={match.team1ShortName}
                                className="match-card__team-logo"
                                onError={(e) => {
                                    e.currentTarget.src = `/logos/teams/placeholder.png`;
                                }}
                            />
                        </div>
                        <div className="match-card__team-info">
                            <span className="match-card__team-short">{match.team1ShortName}</span>
                            <span className="match-card__team-full">{match.team1Name}</span>
                        </div>
                    </div>

                    {/* Show score based on visibility */}
                    <div className="match-card__vs">
                        {isFinished ? (
                            canShowScore ? (
                                <button
                                    className={`match-card__spoiler-toggle ${canShowScore ? 'match-card__spoiler-toggle--revealed' : ''}`}
                                    onClick={handleEyeClick}
                                    aria-label="Hide result"
                                >
                                    <span className={`match-card__score-number ${
                                        match.team1Score! > match.team2Score! ? 'match-card__score-number--winner' : ''
                                    }`}>
                                        {match.team1Score}
                                    </span>

                                    {<span className="match-card__vs-separator" role="separator" />}
                                    <span className={`match-card__score-number ${
                                        match.team2Score! > match.team1Score! ? 'match-card__score-number--winner' : ''
                                    }`}>
                                        {match.team2Score}
                                    </span>
                                </button>
                            ) : (
                                <button
                                    className="match-card__spoiler-toggle"
                                    onClick={handleEyeClick}
                                    aria-label="Show result"
                                >
                                    <VisibilityOffIcon />
                                </button>
                            )
                        ) : (
                            <span className="match-card__vs-text">vs</span>
                        )}
                    </div>

                    {/* Team 2 */}
                    <div className="match-card__team">
                        <div className="match-card__team-logo">
                            <img
                                src={`/logos/teams/${match.team2ShortName.toLowerCase()}.png`}
                                alt={match.team2ShortName}
                                className="match-card__team-logo"
                                onError={(e) => {
                                    e.currentTarget.src = `/logos/teams/placeholder.png`;
                                }}
                            />
                        </div>
                        <div className="match-card__team-info">
                            <span className="match-card__team-short">{match.team2ShortName}</span>
                            <span className="match-card__team-full">{match.team2Name}</span>
                        </div>
                    </div>
                </div>
            </div>

            {isFinished && (
                <button
                    className={`match-card__chevron ${expanded ? 'match-card__chevron--open' : ''}`}
                    onClick={() => setExpanded(!expanded)}
                    aria-label={expanded ? 'Collapse' : 'Expand'}
                >
                    <ArrowDropdownIcon size={28} />
                </button>
            )}


            {/* Expanded details (only shown when expanded) */}
            {isFinished && (
                <div className={`match-card__details ${expanded ? 'match-card__details--open' : ''}`}>
                    <div className="match-card__details-inner">

                        <div className="match-card__games">
                            {Array.from({ length: match.bestOf }, (_, i) => {
                                const game = match.games.find(g => g.gameNumber === i + 1);
                                const canShowGameResult = showingSpoilers || revealedGames.has(i + 1);

                                // Hide unplayed games when all played games are revealed
                                if (!game?.winningTeam && (showingSpoilers || allPlayedRevealed)) return null;

                                const gameWinnerName = game?.winningTeam === 1
                                    ? match.team1ShortName
                                    : game?.winningTeam === 2
                                        ? match.team2ShortName
                                        : null;

                                return (
                                    <div
                                        className="match-card__game"
                                        key={i}
                                        onClick={() => toggleGameSpoiler(i + 1)}
                                    >
                                        <span className="match-card__game-number">{i + 1}</span>
                                        <span className="match-card__game-spoiler">
                                            {canShowGameResult
                                                ? (gameWinnerName ? `${gameWinnerName} wins` : "Not played")
                                                : "Show result"}
                                        </span>
                                        {game?.vodUrl ? (
                                            <a
                                                href={game.vodUrl}
                                                target="_blank"
                                                rel="noopener noreferrer"
                                                className="match-card__game-vod"
                                                title="Watch VOD"
                                                onClick={(e) => e.stopPropagation()}
                                            >
                                                <PlayIcon />
                                            </a>
                                        ) : (
                                            <span className="match-card__game-vod match-card__game-vod--disabled">
                                                <PlayIcon />
                                            </span>
                                        )}
                                    </div>
                                );
                            })}

                            <Link
                                to={`/matches/${match.id}`}
                                className="match-card__details-link"
                            >
                                View full details
                            </Link>
                        </div>

                    </div>
                </div>
            )}
        </article>
    );
}

/** Formats ISO UTC string to local time (e.g. "14:00"). */
function formatTime(isoUtc: string) {
    return new Date(isoUtc).toLocaleTimeString(undefined, {
        hour: "2-digit",
        minute: "2-digit",
    });
}