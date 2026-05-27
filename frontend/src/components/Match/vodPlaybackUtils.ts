import type { GameListItem } from "@/lib/api.ts";
import { isVodOffsetConfigured } from "@/components/Match/vodTimestampUtils.ts";

/** Manual VOD metadata exposed on match detail games. */
export interface GameVodMetadata {
    baseUrl: string | null;
    draftOffsetSeconds: number | null;
    gameStartOffsetSeconds: number | null;
}

export type GameVodDisplayMode = "none" | "single" | "split";

function manualVodFromGame(game: GameListItem) {
    return game.vods?.find((vod) => vod.source === "Manual") ?? null;
}

export function getGameVodMetadata(game: GameListItem): GameVodMetadata | null {
    const manualVod = manualVodFromGame(game);
    const rawBaseUrl = game.vodBaseUrl ?? manualVod?.url ?? null;
    if (!rawBaseUrl && !game.vodUrl)
        return null;

    const baseUrl = stripPlaybackOffset(rawBaseUrl ?? game.vodUrl!);
    const draftOffsetSeconds = game.vodDraftOffsetSeconds ?? manualVod?.draftOffsetSeconds ?? null;
    const gameStartOffsetSeconds = game.vodGameStartOffsetSeconds ?? manualVod?.offsetSeconds ?? null;

    return {
        baseUrl,
        draftOffsetSeconds: isVodOffsetConfigured(draftOffsetSeconds) ? draftOffsetSeconds : null,
        gameStartOffsetSeconds: isVodOffsetConfigured(gameStartOffsetSeconds) ? gameStartOffsetSeconds : null,
    };
}

export function getGameVodDisplayMode(game: GameListItem): GameVodDisplayMode {
    const metadata = getGameVodMetadata(game);
    if (!metadata?.baseUrl)
        return "none";

    const hasDraft = isVodOffsetConfigured(metadata.draftOffsetSeconds);
    const hasGameStart = isVodOffsetConfigured(metadata.gameStartOffsetSeconds);
    if (hasDraft && hasGameStart)
        return "split";

    return "single";
}

/** Builds a playback URL with an explicit start offset (including 0s). */
export function buildVodUrl(baseUrl: string, offsetSeconds: number): string {
    const trimmed = stripPlaybackOffset(baseUrl);
    const separator = trimmed.includes("?") ? "&" : "?";
    return `${trimmed}${separator}t=${offsetSeconds}s`;
}

export function getGameVodLinks(game: GameListItem): {
    mode: GameVodDisplayMode;
    watchUrl: string | null;
    draftUrl: string | null;
    gameStartUrl: string | null;
} {
    const metadata = getGameVodMetadata(game);
    if (!metadata?.baseUrl) {
        return { mode: "none", watchUrl: null, draftUrl: null, gameStartUrl: null };
    }

    const mode = getGameVodDisplayMode(game);
    const draftUrl = isVodOffsetConfigured(metadata.draftOffsetSeconds)
        ? buildVodUrl(metadata.baseUrl, metadata.draftOffsetSeconds)
        : null;
    const gameStartUrl = isVodOffsetConfigured(metadata.gameStartOffsetSeconds)
        ? buildVodUrl(metadata.baseUrl, metadata.gameStartOffsetSeconds)
        : null;

    if (mode === "split" && draftUrl && gameStartUrl) {
        return { mode, watchUrl: null, draftUrl, gameStartUrl };
    }

    const watchUrl = gameStartUrl ?? draftUrl ?? metadata.baseUrl;
    return { mode: "single", watchUrl, draftUrl: null, gameStartUrl: null };
}

/** Removes baked-in playback offsets from YouTube/Twitch URLs (query + hash). */
export function stripPlaybackOffset(url: string): string {
    try {
        const parsed = new URL(url);
        parsed.searchParams.delete("t");
        parsed.searchParams.delete("start");
        parsed.searchParams.delete("time_continue");
        parsed.hash = "";
        return parsed.toString().replace(/\?$/, "");
    } catch {
        return url;
    }
}
