import { GameDraft } from "@/components/Match/GameDraft.tsx";
import { GameScoreboard } from "@/components/Match/GameScoreboard.tsx";
import { PlayIcon } from "@/components/common/Icons.tsx";
import type { GameDetailsDto, GameListItem, MatchDetails } from "@/lib/api.ts";

interface GamePanelProps {
    match: MatchDetails;
    currentGame: GameListItem;
    gameDetails: GameDetailsDto | undefined;
    gameLoading: boolean;
    gameDetailsError: boolean;
}

export function GamePanel({ match, currentGame, gameDetails, gameLoading, gameDetailsError }: GamePanelProps) {
    return (
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
                <div className="match-detail__placeholder-body" role="alert">
                    <span>Could not load game details.</span>
                </div>
            ) : gameDetails ? (
                <>
                    {/* Draft */}
                    <section className="match-detail__section" aria-labelledby="match-detail-draft-heading">
                        <h2 id="match-detail-draft-heading" className="sr-only">Draft</h2>
                        <div className="match-detail__section-header">
                            <span className="match-detail__section-team">
                                <span className="match-detail__section-kda">
                                    {gameDetails.team1Players.reduce((s, p) => s + p.kills, 0)}/
                                    {gameDetails.team1Players.reduce((s, p) => s + p.deaths, 0)}/
                                    {gameDetails.team1Players.reduce((s, p) => s + p.assists, 0)}
                                </span>
                                <span className="match-detail__section-gold">
                                    {formatGold(gameDetails.team1Players.reduce((s, p) => s + p.goldEarned, 0))}
                                </span>
                            </span>
                            <div className="match-detail__section-center">
                                <div className="match-detail__section-vs">
                                    <span className="match-detail__section-vs-team">{match.team1ShortName}</span>
                                    <span className="match-detail__section-vs-sep">vs</span>
                                    <span className="match-detail__section-vs-team">{match.team2ShortName}</span>
                                </div>
                                {gameDetails.gameDurationSeconds != null && (
                                    <span className="match-detail__section-time">
                                        {formatDuration(gameDetails.gameDurationSeconds)}
                                    </span>
                                )}
                            </div>
                            <span className="match-detail__section-team match-detail__section-team--right">
                                <span className="match-detail__section-gold">
                                    {formatGold(gameDetails.team2Players.reduce((s, p) => s + p.goldEarned, 0))}
                                </span>
                                <span className="match-detail__section-kda">
                                    {gameDetails.team2Players.reduce((s, p) => s + p.kills, 0)}/
                                    {gameDetails.team2Players.reduce((s, p) => s + p.deaths, 0)}/
                                    {gameDetails.team2Players.reduce((s, p) => s + p.assists, 0)}
                                </span>
                            </span>
                        </div>
                        <GameDraft draft={gameDetails.draft} />
                    </section>

                    {/* Scoreboard */}
                    <section className="match-detail__section" aria-labelledby="match-detail-scoreboard-heading">
                        <h2 id="match-detail-scoreboard-heading" className="sr-only">Scoreboard</h2>
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
                </>
            ) : null}
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