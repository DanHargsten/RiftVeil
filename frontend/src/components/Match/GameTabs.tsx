import type { GameListItem } from "@/lib/api.ts";
import { tabButtonClass } from "@/components/Match/matchDisplayUtils.ts";

interface GameTabsProps {
    games: GameListItem[];
    currentGame: GameListItem | undefined;
    onSelect: (gameNumber: number) => void;
}

export function GameTabs({ games, currentGame, onSelect }: GameTabsProps) {
    return (
        <div className="match-detail__tabs" role="tablist" aria-label="Games in this match">
            {games.map((game) => (
                <GameTab
                    key={game.id}
                    game={game}
                    isActive={currentGame?.gameNumber === game.gameNumber}
                    onSelect={onSelect}
                />
            ))}
        </div>
    );
}

function GameTab({
    game,
    isActive,
    onSelect,
}: {
    game: GameListItem;
    isActive: boolean;
    onSelect: (gameNumber: number) => void;
}) {
    return (
        <button
            type="button"
            id={`match-detail-tab-${game.id}`}
            role="tab"
            aria-selected={isActive}
            aria-controls="match-detail-game-panel"
            className={tabButtonClass(isActive)}
            onClick={() => onSelect(game.gameNumber)}
        >
            <span className="match-detail__tab-number">Game {game.gameNumber}</span>
        </button>
    );
}
