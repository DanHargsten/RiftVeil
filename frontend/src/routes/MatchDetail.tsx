import { useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { GameDraft } from "@/components/Match/GameDraft.tsx";
import { GameScoreboard } from "@/components/Match/GameScoreboard.tsx";
import { PlayIcon } from "@/components/common/Icons.tsx";
import { LeagueLogo, TeamLogo } from "@/components/common/Logos.tsx";
import { gamesApi, matchesApi } from "@/lib/api.ts";
import "@/styles/game-details.css";

/** Match detail page: full match info with game-by-game breakdown. */
export function MatchDetail() {
    const { id } = useParams<{ id: string }>();
    const location = useLocation();
    const [selectedGame, setSelectedGame] = useState<number>(1);

    // Back link target: optional `state.from` set by MatchCard
    const from = (location.state as { from?: string })?.from ?? "/";
    const backLabel = from.startsWith("/leagues/") ? "← League" : "← Home";

    const { data: match, isLoading, error } = useQuery({
        queryKey: ["match", id],
        queryFn: () => matchesApi.getById(Number(id)),
    });
    
    const playedGames =
        match?.games.filter((game) => game.winningTeam != null) ?? [];
    const currentGame =
        playedGames.find((game) => game.gameNumber === selectedGame)
        ?? playedGames[0];

    const {
        data: gameDetails,
        isLoading: gameLoading,
        isError: gameDetailsError,
    } = useQuery({
        queryKey: ["game-details", currentGame?.id],
        queryFn: () => {
            if (!currentGame) {
                throw new Error("No game selected");
            }
            return gamesApi.getDetails(currentGame.id);
        },
        enabled: !!currentGame,
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
                    <h1 className="sr-only">
                        Match: {match.team1Name} vs {match.team2Name}
                        {tournamentStage ? ` — ${tournamentStage}` : ""}
                    </h1>

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

                {/* ========== GAME TABS (PLAYED GAMES ONLY) ========== */}
                <div
                    className="match-detail__tabs"
                    role="tablist"
                    aria-label="Games in this match"
                >
                    {playedGames.map((game) => {
                        const isActive = currentGame?.gameNumber === game.gameNumber;
                        const winnerShort = getGameWinnerShort(game.winningTeam);

                        return (
                            <button
                                key={game.id}
                                id={`match-detail-tab-${game.id}`}
                                role="tab"
                                aria-selected={isActive}
                                aria-controls="match-detail-game-panel"
                                className={`match-detail__tab ${isActive ? "match-detail__tab--active" : ""}`}
                                onClick={() => setSelectedGame(game.gameNumber)}
                                type="button"
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
                    <div
                        id="match-detail-game-panel"
                        role="tabpanel"
                        aria-labelledby={`match-detail-tab-${currentGame.id}`}
                        className="match-detail__content"
                    >

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

                        {gameLoading ? (
                            <div className="match-detail-loading">
                                <div className="match-detail-loading__spinner" />
                                <span>Loading game data...</span>
                            </div>
                        ) : gameDetailsError ? (
                            <div
                                className="match-detail__placeholder-body"
                                role="alert"
                            >
                                <span>Could not load game details.</span>
                            </div>
                        ) : gameDetails ? (
                            <>
                                {/* Draft */}
                                <section className="match-detail__section" aria-labelledby="match-detail-draft-heading">
                                    <h2 id="match-detail-draft-heading" className="sr-only">
                                        Draft
                                    </h2>
                                    <div className="match-detail__section-header">
                                        <span className="match-detail__section-team">
                                            <span className="match-detail__section-kda">
                                                {gameDetails.team1Players.reduce((sum, player) => sum + player.kills, 0)}/{gameDetails.team1Players.reduce((sum, player) => sum + player.deaths, 0)}/{gameDetails.team1Players.reduce((sum, player) => sum + player.assists, 0)}
                                            </span>
                                            <span className="match-detail__section-gold">
                                                {formatGold(gameDetails.team1Players.reduce((sum, player) => sum + player.goldEarned, 0))}
                                            </span>
                                        </span>
                                        <div className="match-detail__section-center">
                                            <div className="match-detail__section-vs">
                                                <span className="match-detail__section-vs-team">{match.team1ShortName}</span>
                                                <span className="match-detail__section-vs-sep">vs</span>
                                                <span className="match-detail__section-vs-team">{match.team2ShortName}</span>
                                            </div>
                                            {gameDetails.gameDurationSeconds != null && (
                                                <span className="match-detail__section-time">{formatDuration(gameDetails.gameDurationSeconds)}</span>
                                            )}
                                        </div>
                                        <span className="match-detail__section-team match-detail__section-team--right">
                                            <span className="match-detail__section-gold">
                                                {formatGold(gameDetails.team2Players.reduce((sum, player) => sum + player.goldEarned, 0))}
                                            </span>
                                            <span className="match-detail__section-kda">
                                                {gameDetails.team2Players.reduce((sum, player) => sum + player.kills, 0)}/{gameDetails.team2Players.reduce((sum, player) => sum + player.deaths, 0)}/{gameDetails.team2Players.reduce((sum, player) => sum + player.assists, 0)}
                                            </span>
                                        </span>
                                    </div>
                                    <GameDraft draft={gameDetails.draft} />
                                </section>

                                {/* Scoreboard */}
                                <section className="match-detail__section" aria-labelledby="match-detail-scoreboard-heading">
                                    <h2 id="match-detail-scoreboard-heading" className="sr-only">
                                        Scoreboard
                                    </h2>
                                    <GameScoreboard
                                        team1Name={match.team1ShortName}
                                        team2Name={match.team2ShortName}
                                        team1Players={gameDetails.team1Players}
                                        team2Players={gameDetails.team2Players}
                                        team1Stats={gameDetails.team1Stats}
                                        team2Stats={gameDetails.team2Stats}
                                        winningTeam={gameDetails.winningTeam}
                                    />
                                </section>                                
                                
                                {/* Objectives placeholder */}
                                <section className="match-detail__section">
                                    <h2 className="match-detail__section-title">Objectives</h2>
                                    <div className="match-detail__placeholder-body">
                                        <span>Objectives coming soon</span>
                                    </div>
                                </section>
                            </>
                        ) : null }

                        <div className="match-detail__two-col">
                            <section className="match-detail__section">
                                <h2 className="match-detail__section-title">Gold Advantage</h2>
                                <div className="match-detail__placeholder-body">
                                    <span>Gold graph coming soon</span>
                                </div>
                            </section>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}


function formatGold(gold: number): string {
    return gold >= 1000 ? `${(gold / 1000).toFixed(1)}k` : String(gold);
}

function formatDuration(seconds: number): string {
    const minutes = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${minutes}:${secs.toString().padStart(2, "0")}`;
}