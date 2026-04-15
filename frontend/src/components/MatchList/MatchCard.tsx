import { type MatchListItem } from "@/lib/api.ts";
import React from "react";
import { PlayIcon, VisibilityOffIcon, TimeCircle } from "@/components/common/Icons.tsx";
import {Link, useLocation} from "react-router-dom";
import { TeamLogo, LeagueLogo } from "@/components/common/Logos.tsx";

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

export function MatchCard({ match, spoilers, onReveal, onHide }: MatchCardProps) {
    const location = useLocation();
    
    const isFinished = match.status === "Finished";
    const isLive = match.status === "Live";

    const showingSpoilers = spoilers.globalEnabled || spoilers.revealedMatchIds.has(match.id);

    const canShowScore =
        showingSpoilers &&
        isFinished &&
        match.team1Score != null &&
        match.team2Score != null;

    const handleEyeClick = (e: React.MouseEvent) => {
        e.stopPropagation(); // Prevent click from bubbling to any parent link/card handler
        if (showingSpoilers) onHide();
        else onReveal();
    };

    const getStatusClass = () => {
        if (isLive) return "match-card--live";
        if (isFinished) return "match-card--finished";
        if (match.status === "Cancelled") return "match-card--cancelled";
        return "match-card--upcoming";
    };

    // Compare calendar dates (not timestamps) to get whole-day difference
    const getTimeDisplay = () => {
        if (!isFinished) return formatTime(match.startsAtUtc);
        const now = new Date();
        const matchDate = new Date(match.startsAtUtc);
        const diffDays = Math.floor(
            (new Date(now.toDateString()).getTime() - new Date(matchDate.toDateString()).getTime())
            / (1000 * 60 * 60 * 24)
        );
        if (diffDays === 0) return formatTime(match.startsAtUtc);
        if (diffDays < 7) return `${diffDays}d ago`;
        return matchDate.toLocaleDateString(undefined, { month: "short", day: "numeric" });
    };
    
    return (
        <article className={`match-card ${getStatusClass()}`} aria-labelledby={`match-title-${match.id}`}>
            <h3 id={`match-title-${match.id}`} className="sr-only">
                {match.team1ShortName} vs {match.team2ShortName} — {match.leagueShortName}
            </h3>
            
            {/* ========== HEADER ========== */}
            <header className="match-card__header">

                {/* TOURNAMENT */}
                <div className="match-card__tournament">
                    <LeagueLogo shortName={match.leagueShortName} />
                    <span className="match-card__tournament-league">{match.leagueShortName}</span>
                    {match.tournamentStage && (
                        <>
                            <span className="match-card__tournament-separator">·</span>
                            <span className="match-card__tournament-stage">{match.tournamentStage}</span>
                        </>
                    )}
                </div>

                {/* STATUS */}
                <div className="match-card__status">
                    {isLive ? (
                        <span className="badge badge--live" aria-live="polite" role="status">LIVE</span>
                    ) : match.status === "Scheduled" ? (
                        <span className="badge badge--scheduled">
                            <TimeCircle className="badge__icon" size={18} aria-hidden="true" />
                            Upcoming
                        </span>
                    ) : null}
                </div>
            </header>

            {/* ========== TEAMS + SCORE ========== */}
            <div className="match-card__main">   
                
                {/* TEAM 1 */}
                <div className="match-card__team match-card__team--left">
                    <TeamLogo shortName={match.team1ShortName} />
                    <div className="match-card__team-info">
                        <span className="match-card__team-short">{match.team1ShortName}</span>
                        <span className="match-card__team-full">{match.team1Name}</span>
                    </div>
                </div>

                {/* SCORE */}
                <div className="match-card__vs">
                    {isFinished ? (
                        <button
                            className={`match-card__spoiler-toggle ${canShowScore ? "match-card__spoiler-toggle--revealed" : ""}`}
                            onClick={handleEyeClick}
                            aria-expanded={canShowScore}
                            type="button"
                            aria-label={
                                canShowScore
                                    ? `Hide results`
                                    : `Show results`
                            }
                        >
                            {canShowScore ? (
                                <>
                                    <span className="sr-only">Result: </span>
                                    <ScoreNumber score={match.team1Score!} otherScore={match.team2Score!} />
                                    <span className="match-card__vs-separator" aria-hidden="true" />
                                    <span className="sr-only"> to </span>
                                    <ScoreNumber score={match.team2Score!} otherScore={match.team1Score!} />
                                </>
                            ) : (
                                <>
                                    <VisibilityOffIcon size={28} aria-hidden="true" />
                                    <span className="match-card__spoiler-toggle-text">Show score</span>
                                </>
                            )}
                        </button>
                    ) : isLive ? (
                        <span className="match-card__vs-live">LIVE</span>
                    ) : (
                        <span className="match-card__vs-text">vs</span>
                    )}
                </div>

                {/* TEAM 2 */}
                <div className="match-card__team match-card__team--right">
                    <TeamLogo shortName={match.team2ShortName} />
                    <div className="match-card__team-info">
                        <span className="match-card__team-short">{match.team2ShortName}</span>
                        <span className="match-card__team-full">{match.team2Name}</span>
                    </div>
                </div>
            </div>

            {/* ========== FOOTER ========== */}
            <footer className="match-card__footer">
                
                {/* VODS */}
                <div className="match-card__vods">
                    {isFinished ? (
                        <VodButtons match={match} canShowScore={canShowScore} />
                    ) : (
                        <time dateTime={match.startsAtUtc} className="match-card__vods-empty">
                            Starting {getTimeDisplay()}
                        </time>
                    )}
                </div>

                {/* BEST-OF */}
                <span className="match-card__best-of" aria-label={`Best of ${match.bestOf}`}>Bo{match.bestOf}</span>

                {/* DETAILS LINK */}
                {isFinished && (
                    <Link
                        to={`/matches/${match.id}`}
                        state={{ from: location.pathname }}
                        className="match-card__details-link"
                    >
                        View Match Details →
                    </Link>
                )}
            </footer>
        </article>
    );
}

function formatTime(isoUtc: string) {
    return new Date(isoUtc).toLocaleTimeString(undefined, {
        hour: "2-digit",
        minute: "2-digit",
    });
}

function ScoreNumber({ score, otherScore }: { score: number; otherScore: number }) {
    const isWinner = score > otherScore;
    return (
        <span className={`match-card__score-number ${isWinner ? "match-card__score-number--winner" : ""}`}>
            {score}
            {isWinner && <span className="sr-only"> (winner)</span>}
        </span>
    );
}

function VodButtons({ match, canShowScore }: { match: MatchListItem; canShowScore: boolean }) {
    const vodCount = match.games.filter(g => g.vodUrl).length;
    if (vodCount === 0) {
        return <span className="match-card__vods-empty">No VOD available yet</span>;
    }
    // Show only played games when score is visible, show bestOf number of buttons when hidden (avoids spoiling game count)
    const count = canShowScore
        ? match.games.filter(g => g.winningTeam != null).length
        : match.bestOf;

    return (
        <>
            {Array.from({ length: count }, (_, i) => {
                const game = match.games.find(g => g.gameNumber === i + 1);
                return game?.vodUrl ? (
                    <a
                        key={i}
                        href={game.vodUrl}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="match-card__vod-btn"
                        title={`Game ${i + 1}`}
                        aria-label={`Watch Game ${i + 1} VOD`}
                    >
                        <span className="match-card__vod-number">{i + 1}</span>
                        <span className="match-card__vod-play"><PlayIcon size={20} /></span>
                    </a>
                ) : (
                    <button
                        key={i}
                        className="match-card__vod-btn match-card__vod-btn--disabled"
                        aria-label={`Game ${i + 1} – No VOD available yet`}
                        aria-disabled="true"
                    >
                        <span className="match-card__vod-number">{i + 1}</span>
                        <span className="match-card__vod-play"><PlayIcon size={20} aria-hidden="true" /></span>
                    </button>
                );
            })}
        </>
    );
}