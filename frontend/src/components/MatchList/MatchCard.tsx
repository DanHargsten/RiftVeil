import { type MatchListItem } from "@/lib/api.ts";
import React, { useState } from "react";

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
 * MatchCard - Displays individual match information with spoiler protection
 * 
 * Features:
 * - Three-stage expansion: compact -> score revealed -> full details
 * - Dynamic time display based on match age
 * - Mirrored team layout for visual balance
 * - Status-based color coding in header
 */
export function MatchCard({ match, spoilers, onReveal, onHide }: MatchCardProps) {
    const [expanded, setExpanded] = useState(false);

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

    // Dynamic time display based on match age
    // Today: "14:00", This week: "2d ago", Older: "4 nov"
    const getTimeDisplay = () => {
        if (isFinished) {
            const matchDate = new Date(match.startsAtUtc);
            const today = new Date();            
            today.setHours(0, 0, 0, 0);
            matchDate.setHours(0, 0, 0);
            
            const diffDays = Math.floor((today.getTime() - matchDate.getTime()) / (1000 * 60 * 60 * 24));
            
            if (diffDays === 0) return formatTime(match.startsAtUtc);
            if (diffDays < 7) return `${diffDays}d ago`;
            
            return new Date(match.startsAtUtc).toLocaleDateString(undefined, {
                month: "short",
                day: "numeric"
            });
        }
        
        return formatTime(match.startsAtUtc);
    };

    const handleCardClick = () => {
        // Only expand if spoilers are already shown
        if (showingSpoilers && isFinished) {
            setExpanded(!expanded);
        }
    };

    const handleEyeClick = (e: React.MouseEvent) => {
        e.stopPropagation(); // Prevent card expansion when toggling spoiler
        if (showingSpoilers) {
            onHide();
            setExpanded(false);
        } else {
            onReveal();
        }
    };

    // Apply status-specific styling classes
    const getStatusClass = () => {
        if (match.status === "Live") return "match-card--live";
        if (match.status === "Finished") return "match-card--finished";
        if (match.status === "Cancelled") return "match-card--cancelled";
        return "match-card--upcoming";
    };

    
    return (
        <article className={`match-card ${getStatusClass()} ${expanded ? "match-card--expanded" : ""}`}>
            
            {/* Header: Tournament + Status */}
            <div className="match-card__header">
                <div className="match-card__tournament">
                    <span className="tournament-league">{match.leagueShortName}</span>
                    {match.tournamentStage && (
                        <>
                            <span className="tournament-separator">/</span>
                            <span className="tournament-stage">{match.tournamentStage}</span>
                        </>
                    )}
                </div>
                <div className="match-card__status">
                    {isLive ? (
                        <span className="status-badge status-badge--live">LIVE</span>
                    ) : (
                        <span className="match-card__best-of">Bo{match.bestOf}</span>
                    )}
                </div>
            </div>

            {/* Main: Time + Teams (clickable for expansion) */}
            <div className="match-card__main" onClick={handleCardClick}>
                <time className="match-card__time" dateTime={match.startsAtUtc}>
                    {getTimeDisplay()}
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
                            <span className="team-short">{match.team1ShortName}</span>
                            <span className="team-full">{match.team1Name}</span>
                        </div>
                    </div>

                    {/* Always show "/" - show score based on visibility */}
                    <div className="match-card__vs">
                        {canShowScore ? (
                            <>
                                <span className="match-card__score-number">{match.team1Score}</span>
                                {" / "}
                                <span className="match-card__score-number">{match.team2Score}</span>
                            </>
                        ) : (
                            "/"
                        )}
                    </div>

                    {/* Mirror team layouts so logos meet in center */}
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
                            <span className="team-short">{match.team2ShortName}</span>
                            <span className="team-full">{match.team2Name}</span>
                        </div>
                    </div>
                </div>

                {/* Eye icon for spoiler toggle (only for finished matches) */}
                {isFinished && (
                    <button
                        className={`match-card__eye ${showingSpoilers ? "match-card__eye--active" : ""}`}
                        onClick={handleEyeClick}
                        aria-label={showingSpoilers ? "Hide result" : "Show result"}
                        title={showingSpoilers ? "Hide result" : "Show result"}
                    >
                        {showingSpoilers ? "👁️" : "👁️‍🗨️"}
                    </button>
                )}
            </div>

            {/* Expanded details (only shown when expanded) */}
            {expanded && canShowScore && (
                <div className="match-card__details">
                    <div className="match-card__detail-row">
                        <span className="match-card__detail-label">Final Score</span>
                        <span className="match-card__detail-value">
              {match.team1Score} - {match.team2Score}
            </span>
                    </div>

                    {/* Placeholder for game-by-game breakdown */}
                    <div className="match-card__games">
                        <div className="match-card__game">
                            <span className="game-number">Game 1</span>
                            <span className="game-duration">32:45</span>
                        </div>
                        <div className="match-card__game">
                            <span className="game-number">Game 2</span>
                            <span className="game-duration">28:12</span>
                        </div>
                        {match.team1Score === 2 && match.team2Score === 1 && (
                            <div className="match-card__game">
                                <span className="game-number">Game 3</span>
                                <span className="game-duration">41:03</span>
                            </div>
                        )}
                    </div>

                    {/* VoD link placeholder */}
                    <div className="match-card__vod-section">
                        <a href="#" className="match-card__vod-link">
                            📺 Watch VoD
                        </a>
                    </div>

                    <button
                        className="match-card__collapse"
                        onClick={() => setExpanded(false)}
                    >
                        Close ↑
                    </button>
                </div>
            )}
        </article>
    );
}

function formatTime(isoUtc: string) {
    return new Date(isoUtc).toLocaleTimeString(undefined, {
        hour: "2-digit",
        minute: "2-digit",
    });
}