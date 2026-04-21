import type {GameListItem} from "@/lib/api.ts";

interface GameTabsProps {
    games: GameListItem[];
    currentGame: GameListItem | undefined;
    onSelect: (gameNumber: number) => void;
    getWinnerShort: (winningTeam: number | null) => string | null;
}

export function GameTabs({ games, currentGame, onSelect, getWinnerShort }: GameTabsProps) {
    return (
        <div className="match-detail__tabs" role="tablist" aria-label="Games in this match">
            {games.map((game) => {
                const isActive = currentGame?.gameNumber === game.gameNumber;
                const winnerShort = getWinnerShort(game.winningTeam);

                return (
                    <button
                        key={game.id}
                        id={`match-detail-tab-${game.id}`}
                        role="tab"
                        aria-selected={isActive}
                        aria-controls="match-detail-game-panel"
                        className={`match-detail__tab ${isActive ? "match-detail__tab--active" : ""}`}
                        onClick={() => onSelect(game.gameNumber)}
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
    );
}
