import { useParams, Link, useLocation } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { matchesApi } from "@/lib/api.ts";
import { useState } from "react";
import { TeamLogo, LeagueLogo } from "@/components/common/Logos.tsx";
import { PlayIcon } from "@/components/common/Icons.tsx";

/** Match detail page: full match info with game-by-game breakdown. */
export function MatchDetail() {
    const { id } = useParams<{ id: string }>();
    const location = useLocation();
    const [selectedGame, setSelectedGame] = useState<number>(1);

    // Läs av vart användaren kom ifrån — skickades med av MatchCard
    const from = (location.state as { from?: string })?.from ?? "/";
    const backLabel = from.startsWith("/leagues/") ? "← League" : "← Home";

    const { data: match, isLoading, error } = useQuery({
        queryKey: ["match", id],
        queryFn: () => matchesApi.getById(Number(id)),
    });

    if (isLoading) {
        return (
            <div className="page">
                <div className="match-detail-loading">
                    <div className="match-detail-loading__spinner" />
                    <span>Loading match...</span>
                </div>
            </div>
        );
    }

    if (error || !match) {
        return (
            <div className="page">
                <div className="match-detail-error">
                    <span>Match not found.</span>
                    <Link to={from} className="match-detail-error__back">← Back</Link>
                </div>
            </div>
        );
    }

    const team1Wins = match.team1Score ?? 0;
    const team2Wins = match.team2Score ?? 0;
    const team1IsWinner = team1Wins > team2Wins;
    const team2IsWinner = team2Wins > team1Wins;

    const leagueShortName = match.tournament.leagueShortName;
    const tournamentStage = match.tournament.stage;

    // Visa bara games som faktiskt spelades
    const playedGames = match.games.filter(g => g.winningTeam != null);

    const currentGame = playedGames.find((g) => g.gameNumber === selectedGame)
        ?? playedGames[0];

    const getGameWinnerShort = (winningTeam: number | null) => {
        if (winningTeam === 1) return match.team1ShortName;
        if (winningTeam === 2) return match.team2ShortName;
        return null;
    };

    return (
        <div className="page">
            <div className="match-detail">

                {/* ── HERO HEADER ── */}
                <header className="match-detail__hero">

                    <div className="match-detail__breadcrumb">
                        <Link to={from} className="match-detail__back-link">
                            {backLabel}
                        </Link>
                        <div className="match-detail__league-info">
                            <LeagueLogo shortName={leagueShortName} className="match-detail__league-logo" />
                            <span className="match-detail__league-name">{leagueShortName}</span>
                            {tournamentStage && (
                                <>
                                    <span className="match-detail__meta-sep">·</span>
                                    <span className="match-detail__tournament-stage">{tournamentStage}</span>
                                </>
                            )}
                            <span className="match-detail__meta-sep">·</span>
                            <span className="match-detail__best-of">Best of {match.bestOf}</span>
                        </div>
                    </div>

                    {/* Teams + Score */}
                    <div className="match-detail__scoreline">

                        {/* Team 1 */}
                        <div className={`match-detail__team match-detail__team--left ${team1IsWinner ? "match-detail__team--winner" : team2IsWinner ? "match-detail__team--loser" : ""}`}>
                            <div className="match-detail__team-identity">
                                <span className="match-detail__team-short">{match.team1ShortName}</span>
                                <span className="match-detail__team-full">{match.team1Name}</span>
                            </div>
                            <div className="match-detail__team-logo-wrap">
                                <TeamLogo shortName={match.team1ShortName} className="match-detail__team-logo" />
                            </div>
                        </div>

                        {/* Score */}
                        <div className="match-detail__score-block">
                            <div className="match-detail__score">
                                <span className={`match-detail__score-num ${team1IsWinner ? "match-detail__score-num--winner" : ""}`}>
                                    {team1Wins}
                                </span>
                                <span className="match-detail__score-divider">–</span>
                                <span className={`match-detail__score-num ${team2IsWinner ? "match-detail__score-num--winner" : ""}`}>
                                    {team2Wins}
                                </span>
                            </div>
                            {(team1IsWinner || team2IsWinner) && (
                                <span className="match-detail__winner-label">
                                    {team1IsWinner ? match.team1ShortName : match.team2ShortName} wins
                                </span>
                            )}
                        </div>

                        {/* Team 2 */}
                        <div className={`match-detail__team match-detail__team--right ${team2IsWinner ? "match-detail__team--winner" : team1IsWinner ? "match-detail__team--loser" : ""}`}>
                            <div className="match-detail__team-logo-wrap">
                                <TeamLogo shortName={match.team2ShortName} className="match-detail__team-logo" />
                            </div>
                            <div className="match-detail__team-identity">
                                <span className="match-detail__team-short">{match.team2ShortName}</span>
                                <span className="match-detail__team-full">{match.team2Name}</span>
                            </div>
                        </div>
                    </div>
                </header>

                {/* ── GAME TABS — bara spelade games ── */}
                <div className="match-detail__tabs">
                    {playedGames.map((game) => {
                        const isActive = currentGame?.gameNumber === game.gameNumber;
                        const winnerShort = getGameWinnerShort(game.winningTeam);

                        return (
                            <button
                                key={game.id}
                                className={`match-detail__tab ${isActive ? "match-detail__tab--active" : ""}`}
                                onClick={() => setSelectedGame(game.gameNumber)}
                                type="button"
                                aria-selected={isActive}
                            >
                                <span className="match-detail__tab-number">Game {game.gameNumber}</span>
                                {winnerShort && (
                                    <span className="match-detail__tab-winner">{winnerShort} win</span>
                                )}
                            </button>
                        );
                    })}
                </div>

                {/* ── GAME CONTENT ── */}
                {currentGame && (
                    <div className="match-detail__content">

                        {currentGame.vodUrl ? (
                            <a
                                href={currentGame.vodUrl}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="match-detail__vod-btn"
                                aria-label={`Watch Game ${currentGame.gameNumber} VOD`}
                            >
                                <PlayIcon size={16} aria-hidden="true" />
                                Watch Game {currentGame.gameNumber} VOD
                            </a>
                        ) : (
                            <span className="match-detail__vod-unavailable">No VOD available for this game</span>
                        )}

                        <section className="match-detail__section">
                            <h2 className="match-detail__section-title">Draft</h2>
                            <div className="match-detail__placeholder-body">
                                <span>Draft data coming soon</span>
                            </div>
                        </section>

                        <section className="match-detail__section">
                            <h2 className="match-detail__section-title">Scoreboard</h2>
                            <div className="match-detail__placeholder-body">
                                <span>Player stats coming soon</span>
                            </div>
                        </section>

                        <div className="match-detail__two-col">
                            <section className="match-detail__section">
                                <h2 className="match-detail__section-title">Gold Advantage</h2>
                                <div className="match-detail__placeholder-body">
                                    <span>Gold graph coming soon</span>
                                </div>
                            </section>
                            <section className="match-detail__section">
                                <h2 className="match-detail__section-title">Objectives</h2>
                                <div className="match-detail__placeholder-body">
                                    <span>Objectives coming soon</span>
                                </div>
                            </section>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}