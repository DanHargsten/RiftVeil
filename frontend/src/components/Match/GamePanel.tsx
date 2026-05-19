import { GameDraft } from "@/components/Match/GameDraft.tsx";
import { GameObjectives } from "@/components/Match/GameObjectives.tsx";
import { GameScoreboard } from "@/components/Match/GameScoreboard.tsx";
import { PlayIcon } from "@/components/common/Icons.tsx";
import { TeamLogo } from "@/components/common/Logos.tsx";
import statGoldIcon from "@/assets/icons/lol-icons/lol-stat-gold.png";
import statKdaIcon from "@/assets/icons/lol-icons/lol-stat-kda.png";
import type { GameDetailsDto, GameListItem, MatchDetails } from "@/lib/api.ts";

export interface GamePanelDevImportProps {
    busy: boolean;
    error: string | null;
    onImport: () => void;
}

interface GamePanelProps {
    match: MatchDetails;
    currentGame: GameListItem;
    gameDetails: GameDetailsDto | undefined;
    gameLoading: boolean;
    gameDetailsError: boolean;
    /** Dev-only: Leaguepedia import for this game id (see MatchDetail). */
    devImport?: GamePanelDevImportProps;
}

export function GamePanel({
    match,
    currentGame,
    gameDetails,
    gameLoading,
    gameDetailsError,
    devImport,
}: GamePanelProps) {
    return (
        <div
            id="match-detail-game-panel"
            role="tabpanel"
            aria-labelledby={`match-detail-tab-${currentGame.id}`}
            className="match-detail__content"
        >
            <div className="match-detail__vod-row">
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
                {devImport ? (
                    <button
                        type="button"
                        className="match-detail__dev-import-btn"
                        disabled={devImport.busy}
                        onClick={devImport.onImport}
                        aria-label={`Import Leaguepedia details for game ${currentGame.gameNumber}`}
                    >
                        {devImport.busy ? "Importing…" : "Import details"}
                    </button>
                ) : null}
            </div>
            {devImport?.error ? (
                <p className="match-detail__dev-import-error" role="alert">
                    {devImport.error}
                </p>
            ) : null}

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
                            <span className="match-detail__section-team match-detail__section-team--with-logo">
                                <TeamLogo shortName={match.team1ShortName} className="match-detail__section-team-logo" />
                                <span className="match-detail__section-kda" aria-label={`${match.team1ShortName} kills, deaths and assists`}>
                                    <img src={statKdaIcon} alt="" aria-hidden="true" className="match-detail__section-stat-icon" />
                                    <span>
                                        {gameDetails.team1Players.reduce((s, p) => s + p.kills, 0)}/
                                        {gameDetails.team1Players.reduce((s, p) => s + p.deaths, 0)}/
                                        {gameDetails.team1Players.reduce((s, p) => s + p.assists, 0)}
                                    </span>
                                </span>
                                <span className="match-detail__section-gold" aria-label={`${match.team1ShortName} total gold`}>
                                    <img src={statGoldIcon} alt="" aria-hidden="true" className="match-detail__section-stat-icon" />
                                    <span>{formatGold(gameDetails.team1Players.reduce((s, p) => s + p.goldEarned, 0))}</span>
                                </span>
                                {gameDetails.winningTeam === 1 ? (
                                    <span className="match-detail__section-win-badge">WIN</span>
                                ) : null}
                            </span>
                            <div className="match-detail__section-center">
                                <span className="match-detail__section-game-label">
                                    Game {currentGame.gameNumber}
                                </span>
                                {gameDetails.gameDurationSeconds != null && (
                                    <span className="match-detail__section-time">
                                        {formatDuration(gameDetails.gameDurationSeconds)}
                                    </span>
                                )}
                            </div>
                            <span className="match-detail__section-team match-detail__section-team--right match-detail__section-team--with-logo">
                                {gameDetails.winningTeam === 2 ? (
                                    <span className="match-detail__section-win-badge">WIN</span>
                                ) : null}
                                <span className="match-detail__section-gold" aria-label={`${match.team2ShortName} total gold`}>
                                    <img src={statGoldIcon} alt="" aria-hidden="true" className="match-detail__section-stat-icon" />
                                    <span>{formatGold(gameDetails.team2Players.reduce((s, p) => s + p.goldEarned, 0))}</span>
                                </span>
                                <span className="match-detail__section-kda" aria-label={`${match.team2ShortName} kills, deaths and assists`}>
                                    <img src={statKdaIcon} alt="" aria-hidden="true" className="match-detail__section-stat-icon" />
                                    <span>
                                        {gameDetails.team2Players.reduce((s, p) => s + p.kills, 0)}/
                                        {gameDetails.team2Players.reduce((s, p) => s + p.deaths, 0)}/
                                        {gameDetails.team2Players.reduce((s, p) => s + p.assists, 0)}
                                    </span>
                                </span>
                                <TeamLogo shortName={match.team2ShortName} className="match-detail__section-team-logo" />
                            </span>
                        </div>
                        <GameDraft
                            draft={gameDetails.draft}
                            team1Side={gameDetails.team1Side}
                        />
                        <div className="match-detail__section-divider" aria-hidden="true" />
                        <h2 id="match-detail-scoreboard-heading" className="sr-only">Scoreboard</h2>
                        <section
                            className="match-detail__section match-detail__section--compact"
                            aria-labelledby="match-detail-objectives-title"
                        >
                            <h3 id="match-detail-objectives-title" className="match-detail__section-title">
                                Global objectives
                            </h3>
                            <GameObjectives
                                match={match}
                                gameDetails={gameDetails}
                                loading={gameLoading}
                                error={gameDetailsError}
                            />
                        </section>
                        <div className="match-detail__section-divider" aria-hidden="true" />
                        <GameScoreboard
                            team1Players={gameDetails.team1Players}
                            team2Players={gameDetails.team2Players}
                            team1Side={gameDetails.team1Side}
                            showDamage={false}
                        />
                        <section
                            className="match-detail__section match-detail__section--compact"
                            aria-labelledby="match-detail-highlights-title"
                        >
                            <h3 id="match-detail-highlights-title" className="match-detail__section-title">
                                Highlights
                            </h3>
                            <div className="match-detail__placeholder-body match-detail__placeholder-body--compact">
                                <span>Coming soon</span>
                            </div>
                        </section>
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