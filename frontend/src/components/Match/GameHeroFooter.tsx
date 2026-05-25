import { GameTabs } from "@/components/Match/GameTabs.tsx";
import { PlayIcon } from "@/components/common/Icons.tsx";
import { getGameVodLinks } from "@/components/Match/vodPlaybackUtils.ts";
import type { GameListItem } from "@/lib/api.ts";

interface GameHeroFooterProps {
    games: GameListItem[];
    currentGame: GameListItem | undefined;
    onSelect: (gameNumber: number) => void;
}

export function GameHeroFooter({ games, currentGame, onSelect }: GameHeroFooterProps) {
    return (
        <div className="match-detail__hero-footer-inner">
            <GameTabs games={games} currentGame={currentGame} onSelect={onSelect} />
            {currentGame ? <GameHeroActions game={currentGame} /> : null}
        </div>
    );
}

function GameHeroActions({ game }: { game: GameListItem }) {
    return (
        <div className="match-detail__hero-actions">
            <GameVodLinks game={game} />
        </div>
    );
}

function GameVodLinks({ game }: { game: GameListItem }) {
    const { mode, watchUrl, draftUrl, gameStartUrl } = getGameVodLinks(game);
    const gameLabel = `Game ${game.gameNumber}`;

    if (mode === "none") {
        return (
            <span className="match-detail__vod-unavailable match-detail__vod-unavailable--hero">
                No VOD for this game
            </span>
        );
    }

    if (mode === "split" && draftUrl && gameStartUrl) {
        return (
            <div
                className="match-detail__vod-group match-detail__vod-group--hero"
                role="group"
                aria-label={`Watch ${gameLabel} VOD`}
            >
                <span className="match-detail__vod-group-label">
                    <PlayIcon size={14} className="match-detail__vod-group-icon" aria-hidden="true" />
                    Watch VOD
                </span>
                <VodLink
                    href={draftUrl}
                    label={`Watch ${gameLabel} VOD from draft phase`}
                    text="Draft phase"
                    chip
                />
                <VodLink
                    href={gameStartUrl}
                    label={`Watch ${gameLabel} VOD from game start`}
                    text="Game start"
                    chip
                />
            </div>
        );
    }

    if (!watchUrl)
        return null;

    return (
        <VodLink
            href={watchUrl}
            label={`Watch ${gameLabel} VOD`}
            text="Watch VOD"
            showIcon
        />
    );
}

function VodLink({
    href,
    label,
    text,
    showIcon = false,
    chip = false,
}: {
    href: string;
    label: string;
    text: string;
    showIcon?: boolean;
    chip?: boolean;
}) {
    return (
        <a
            href={href}
            target="_blank"
            rel="noopener noreferrer"
            className={[
                "match-detail__vod-btn",
                "match-detail__vod-btn--hero",
                chip ? "match-detail__vod-btn--chip" : "",
            ].filter(Boolean).join(" ")}
            aria-label={label}
        >
            {showIcon ? (
                <PlayIcon size={14} className="match-detail__vod-btn-icon" aria-hidden="true" />
            ) : null}
            {text}
        </a>
    );
}
