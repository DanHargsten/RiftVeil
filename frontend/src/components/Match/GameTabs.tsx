import type { GameListItem } from "@/lib/api.ts";

interface GameTabsProps {
    games: GameListItem[];
    currentGame: GameListItem | undefined;
    onSelect: (gameNumber: number) => void;
}

export function GameTabs({ games, currentGame, onSelect }: GameTabsProps) {
    return (
        <div className="match-detail__tabs" role="tablist" aria-label="Games in this match">
            {games.map((game) => {
                const isActive = currentGame?.gameNumber === game.gameNumber;

                return (
                    <button
                        key={game.id}
                        type="button"
                        id={`match-detail-tab-${game.id}`}
                        role="tab"
                        aria-selected={isActive}
                        aria-controls="match-detail-game-panel"
                        className={`match-detail__tab ${isActive ? "match-detail__tab--active" : ""}`}
                        onClick={() => onSelect(game.gameNumber)}
                    >
                        <span className="match-detail__tab-number">Game {game.gameNumber}</span>
                    </button>
                );
            })}
        </div>
    );
}
