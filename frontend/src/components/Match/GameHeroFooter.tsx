import { GameTabs } from "@/components/Match/GameTabs.tsx";
import { PlayIcon } from "@/components/common/Icons.tsx";
import type { GameListItem } from "@/lib/api.ts";

export interface GameHeroFooterDevImport {
    busy: boolean;
    error: string | null;
    onImport: () => void;
}

interface GameHeroFooterProps {
    games: GameListItem[];
    currentGame: GameListItem | undefined;
    onSelect: (gameNumber: number) => void;
    devImport?: GameHeroFooterDevImport;
}

export function GameHeroFooter({ games, currentGame, onSelect, devImport }: GameHeroFooterProps) {
    return (
        <div className="match-detail__hero-footer-inner">
            <GameTabs games={games} currentGame={currentGame} onSelect={onSelect} />
            {currentGame ? <GameHeroActions game={currentGame} devImport={devImport} /> : null}
            {devImport?.error ? (
                <p className="match-detail__dev-import-error" role="alert">
                    {devImport.error}
                </p>
            ) : null}
        </div>
    );
}

function GameHeroActions({
    game,
    devImport,
}: {
    game: GameListItem;
    devImport?: GameHeroFooterDevImport;
}) {
    return (
        <div className="match-detail__hero-actions">
            {devImport ? <DevImportButton gameNumber={game.gameNumber} devImport={devImport} /> : null}
            <GameVodLink game={game} />
        </div>
    );
}

function DevImportButton({
    gameNumber,
    devImport,
}: {
    gameNumber: number;
    devImport: GameHeroFooterDevImport;
}) {
    const label = devImport.busy ? "Importing…" : "Import details";

    return (
        <button
            type="button"
            className="match-detail__dev-import-btn"
            disabled={devImport.busy}
            onClick={devImport.onImport}
            aria-label={`Import Leaguepedia details for game ${gameNumber}`}
        >
            {label}
        </button>
    );
}

function GameVodLink({ game }: { game: GameListItem }) {
    if (!game.vodUrl) {
        return (
            <span className="match-detail__vod-unavailable match-detail__vod-unavailable--hero">
                No VOD for this game
            </span>
        );
    }

    return (
        <a
            href={game.vodUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="match-detail__vod-btn match-detail__vod-btn--hero"
            aria-label={`Watch Game ${game.gameNumber} VOD`}
        >
            <PlayIcon size={14} className="match-detail__vod-btn-icon" aria-hidden="true" />
            Watch Game {game.gameNumber} VOD
        </a>
    );
}
