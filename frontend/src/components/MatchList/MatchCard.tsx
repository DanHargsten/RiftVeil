import type { MouseEvent } from "react";
import { Link, useLocation } from "react-router-dom";
import { PlayIcon, TimeCircle, VisibilityOffIcon } from "@/components/common/Icons.tsx";
import { LeagueLogo, TeamLogo } from "@/components/common/Logos.tsx";
import { formatTeamDisplayNames, isTbdTeam } from "@/lib/teamDisplayUtils.ts";
import type { MatchListItem } from "@/lib/api.ts";

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
    const team1IsWinner = canShowScore ? match.team1Score! > match.team2Score! : false;
    const team2IsWinner = canShowScore ? match.team2Score! > match.team1Score! : false;

    const handleEyeClick = (e: MouseEvent<HTMLButtonElement>) => {
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

    const team1Display = formatTeamDisplayNames(match.team1ShortName, match.team1Name);
    const team2Display = formatTeamDisplayNames(match.team2ShortName, match.team2Name);

    return (
        <article className={`match-card ${getStatusClass()}`} aria-labelledby={`match-title-${match.id}`}>
            <h3 id={`match-title-${match.id}`} className="sr-only">
                {team1Display.short} vs {team2Display.short} — {match.leagueShortName}
            </h3>
            
            {/* ========== HEADER ========== */}
            <header className="match-card__header">

                {/* TOURNAMENT */}
                <div className="match-card__tournament">
                    <Link
                        to={`/leagues/${match.leagueShortName.toLowerCase()}`}
                        className="match-card__league-link"
                        aria-label={`Go to ${match.leagueShortName} league`}
                    >
                        <LeagueLogo shortName={match.leagueShortName} />
                        <span className="match-card__tournament-league">{match.leagueShortName}</span>
                    </Link>
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
                        <span className="badge badge--live" aria-live="polite" role="status">
                            <span className="badge__pulse" aria-hidden="true" />
                            LIVE
                        </span>
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
                <div className={`match-card__team match-card__team--left${
                    isTbdTeam(match.team1ShortName) ? " match-card__team--tbd" : ""
                } ${
                    team1IsWinner ? "match-card__team--winner" : team2IsWinner ? "match-card__team--loser" : ""
                }`}>
                    <TeamLogo
                        shortName={match.team1ShortName}
                        logoUrl={match.team1LogoUrl}
                        iconLogoUrl={match.team1IconLogoUrl}
                    />
                    <div className="match-card__team-info">
                        <span className="match-card__team-short">{team1Display.short}</span>
                        <span className="match-card__team-full">{team1Display.full}</span>
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
                    ) : (
                        <>
                            {/* Same "vs" for Scheduled and Live — live state is already clear from the badge, border, and Watch live CTA. */}
                            <span className="match-card__vs-text">vs</span>
                        </>
                    )}
                </div>

                {/* TEAM 2 */}
                <div className={`match-card__team match-card__team--right${
                    isTbdTeam(match.team2ShortName) ? " match-card__team--tbd" : ""
                } ${
                    team2IsWinner ? "match-card__team--winner" : team1IsWinner ? "match-card__team--loser" : ""
                }`}>
                    <TeamLogo
                        shortName={match.team2ShortName}
                        logoUrl={match.team2LogoUrl}
                        iconLogoUrl={match.team2IconLogoUrl}
                    />
                    <div className="match-card__team-info">
                        <span className="match-card__team-short">{team2Display.short}</span>
                        <span className="match-card__team-full">{team2Display.full}</span>
                    </div>
                </div>
            </div>

            {/* ========== FOOTER ========== */}
            <footer className="match-card__footer">

                {/* VODS / WATCH LIVE / STARTING TIME */}
                <div className="match-card__vods">
                    {isFinished ? (
                        <VodButtons match={match} canShowScore={canShowScore} />
                    ) : isLive ? (
                        <a
                            href={buildLolesportsLiveUrl(match.leagueShortName)}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="match-card__watch-live"
                            aria-label={`Watch ${match.leagueShortName} live on lolesports.com (opens in new tab)`}
                        >
                            <PlayIcon size={15} aria-hidden="true" />
                            Watch live
                        </a>
                    ) : (
                        <time dateTime={match.startsAtUtc} className="match-card__start-time">
                            Starting
                            <span className="match-card__start-time-value">{getTimeDisplay()}</span>
                        </time>
                    )}
                </div>

                {/* ROUND + BEST-OF (this-match metadata) */}
                <div className="match-card__match-meta">
                    {match.round && (
                        <>
                            <span className="match-card__round">{match.round}</span>
                            <span className="match-card__match-meta-separator">·</span>
                        </>
                    )}
                    <span className="match-card__best-of" aria-label={`Best of ${match.bestOf}`}>Bo{match.bestOf}</span>
                </div>

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

/**
 * Lolesports live page follows the pattern /live/{slug}/{slug} with slug = lowercase
 * league short name (lck, lec, lcs, lpl, ...). If a future league breaks this pattern
 * I can either special-case it here or add a per-league override on the backend DTO.
 */
function buildLolesportsLiveUrl(leagueShortName: string): string {
    const slug = leagueShortName.toLowerCase();
    return `https://lolesports.com/live/${slug}/${slug}`;
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
    const vodCount = match.games.filter((game) => game.vodUrl).length;
    if (vodCount === 0) {
        return <span className="match-card__vods-empty">No VOD available yet</span>;
    }
    // When score is visible, show one slot per played game; when hidden, show bestOf slots to avoid spoiling game count.
    const count = canShowScore
        ? match.games.filter((game) => game.winningTeam != null).length
        : match.bestOf;

    return (
        <>
            <span className="match-card__vods-label">Watch VODs:</span>
            {Array.from({ length: count }, (_, i) => {
                const game = match.games.find((listedGame) => listedGame.gameNumber === i + 1);
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
                        <span className="match-card__vod-play"><PlayIcon size={18} aria-hidden="true" /></span>
                    </a>
                ) : (
                    <button
                        key={i}
                        className="match-card__vod-btn match-card__vod-btn--disabled"
                        aria-label={`Game ${i + 1} – No VOD available yet`}
                        aria-disabled="true"
                    >
                        <span className="match-card__vod-number">{i + 1}</span>
                        <span className="match-card__vod-play"><PlayIcon size={16} aria-hidden="true" /></span>
                    </button>
                );
            })}
        </>
    );
}