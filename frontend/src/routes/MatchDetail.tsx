import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { GameHeroFooter } from "@/components/Match/GameHeroFooter.tsx";
import { GamePanel } from "@/components/Match/GamePanel.tsx";
import { MatchDevMenu, type MatchDevVodDraft } from "@/components/Match/MatchDevMenu.tsx";
import { MatchHero } from "@/components/Match/MatchHero.tsx";
import { stripPlaybackOffset } from "@/components/Match/vodPlaybackUtils.ts";
import {
    formatVodTimestamp,
    isVodOffsetConfigured,
    parseVodTimestamp,
    vodTimestampFieldError,
} from "@/components/Match/vodTimestampUtils.ts";
import { gamesApi, matchesApi, type GameListItem, type MatchDetails } from "@/lib/api.ts";

function emptyVodDraft(): MatchDevVodDraft {
    return { url: "", draftOffsetSeconds: "", gameStartOffsetSeconds: "" };
}

function vodDraftFromGame(game: GameListItem): MatchDevVodDraft {
    const manualVod = game.vods?.find((vod) => vod.locale === "manual") ?? null;
    const draftOffset = game.vodDraftOffsetSeconds ?? manualVod?.draftOffsetSeconds;
    const gameStartOffset = game.vodGameStartOffsetSeconds ?? manualVod?.offsetSeconds;

    return {
        url: stripPlaybackOffset(game.vodBaseUrl ?? manualVod?.url ?? game.vodUrl ?? ""),
        draftOffsetSeconds: isVodOffsetConfigured(draftOffset)
            ? formatVodTimestamp(draftOffset)
            : "",
        gameStartOffsetSeconds: isVodOffsetConfigured(gameStartOffset)
            ? formatVodTimestamp(gameStartOffset)
            : "",
    };
}

function mergeSavedVodIntoMatch(
    match: MatchDetails,
    gameId: number,
    saved: {
        vodUrl: string | null;
        baseUrl: string | null;
        draftOffsetSeconds: number | null;
        gameStartOffsetSeconds: number | null;
    },
): MatchDetails {
    return {
        ...match,
        games: match.games.map((game) => {
            if (game.id !== gameId)
                return game;

            const manualVod = game.vods?.find((vod) => vod.locale === "manual");
            const nextManualVod = manualVod
                ? {
                    ...manualVod,
                    url: saved.baseUrl ?? manualVod.url,
                    draftOffsetSeconds: saved.draftOffsetSeconds,
                    offsetSeconds: saved.gameStartOffsetSeconds,
                }
                : null;

            return {
                ...game,
                vodUrl: saved.vodUrl,
                vodBaseUrl: saved.baseUrl,
                vodDraftOffsetSeconds: saved.draftOffsetSeconds,
                vodGameStartOffsetSeconds: saved.gameStartOffsetSeconds,
                vods: nextManualVod
                    ? (game.vods ?? []).map((vod) => (vod.locale === "manual" ? nextManualVod : vod))
                    : game.vods?.filter((vod) => vod.locale !== "manual") ?? null,
            };
        }),
    };
}

async function importGameDetails(gameId: number): Promise<string | null> {
    const res = await fetch(`/api/import/game-details/game/${gameId}`, { method: "POST" });
    const text = (await res.text()).trim();
    if (!res.ok)
        return text || `Import failed (${res.status})`;
    return null;
}

/** Match detail page: full match info with game-by-game breakdown. */
export function MatchDetail() {
    const { id } = useParams<{ id: string }>();
    const location = useLocation();
    const queryClient = useQueryClient();
    const [selectedGame, setSelectedGame] = useState<number>(1);
    const [devBusy, setDevBusy] = useState(false);
    const [devError, setDevError] = useState<string | null>(null);
    const [devMessage, setDevMessage] = useState<string | null>(null);
    const [vodDraft, setVodDraft] = useState<MatchDevVodDraft>(emptyVodDraft);

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

    useEffect(() => {
        setDevError(null);
        setDevMessage(null);
        if (currentGame)
            setVodDraft(vodDraftFromGame(currentGame));
    }, [
        currentGame?.id,
        currentGame?.vodBaseUrl,
        currentGame?.vodUrl,
        currentGame?.vodDraftOffsetSeconds,
        currentGame?.vodGameStartOffsetSeconds,
        currentGame?.vods,
    ]);

    const { data: gameDetails, isLoading: gameLoading, isError: gameDetailsError } = useQuery({
        queryKey: ["game-details", currentGame?.id],
        queryFn: () => {
            if (!currentGame) throw new Error("No game selected");
            return gamesApi.getDetails(currentGame.id);
        },
        enabled: !!currentGame,
    });

    if (isLoading) {
        return (
            <div className="page" role="status" aria-live="polite">
                <div className="match-detail-loading">
                    <div className="match-detail-loading__spinner" aria-hidden="true" />
                    <span>Loading match...</span>
                </div>
            </div>
        );
    }

    if (error || !match) {
        return (
            <div className="page" role="alert">
                <div className="match-detail-error">
                    <span>Match not found.</span>
                    <Link to={from} className="match-detail-error__back">← Back</Link>
                </div>
            </div>
        );
    }

    async function handleDevImportSeries() {
        if (playedGames.length === 0 || devBusy) return;
        setDevBusy(true);
        setDevError(null);
        setDevMessage(null);
        try {
            for (const game of playedGames) {
                const importError = await importGameDetails(game.id);
                if (importError) {
                    setDevError(`Game ${game.gameNumber}: ${importError}`);
                    return;
                }
            }
            setDevMessage(`Imported details for ${playedGames.length} game(s).`);
            await queryClient.invalidateQueries({ queryKey: ["game-details"] });
        } catch (err) {
            setDevError(err instanceof Error ? err.message : "Import failed");
        } finally {
            setDevBusy(false);
        }
    }

    async function handleSaveVod() {
        if (!currentGame || devBusy) return;

        const draftOffsetSeconds = parseVodTimestamp(vodDraft.draftOffsetSeconds);
        const gameStartOffsetSeconds = parseVodTimestamp(vodDraft.gameStartOffsetSeconds);
        if (draftOffsetSeconds === undefined) {
            setDevError(vodTimestampFieldError("Draft start"));
            return;
        }
        if (gameStartOffsetSeconds === undefined) {
            setDevError(vodTimestampFieldError("Game start"));
            return;
        }

        setDevBusy(true);
        setDevError(null);
        setDevMessage(null);
        try {
            const url = vodDraft.url.trim();
            const result = await gamesApi.updateVod(currentGame.id, {
                url: url.length > 0 ? url : null,
                draftOffsetSeconds,
                gameStartOffsetSeconds,
            });
            const savedDraft = {
                url: result.baseUrl ?? result.vodUrl ?? "",
                draftOffsetSeconds: isVodOffsetConfigured(result.draftOffsetSeconds)
                    ? formatVodTimestamp(result.draftOffsetSeconds)
                    : "",
                gameStartOffsetSeconds: isVodOffsetConfigured(result.gameStartOffsetSeconds)
                    ? formatVodTimestamp(result.gameStartOffsetSeconds)
                    : "",
            };
            setVodDraft(savedDraft);
            queryClient.setQueryData<MatchDetails>(["match", id], (current) => {
                if (!current) return current;
                return mergeSavedVodIntoMatch(current, currentGame.id, {
                    vodUrl: result.vodUrl,
                    baseUrl: result.baseUrl,
                    draftOffsetSeconds: result.draftOffsetSeconds,
                    gameStartOffsetSeconds: result.gameStartOffsetSeconds,
                });
            });
            setDevMessage(url.length > 0 ? "VOD saved." : "VOD cleared.");
            await queryClient.invalidateQueries({ queryKey: ["match", id] });
            await queryClient.invalidateQueries({ queryKey: ["matches"] });
        } catch (err) {
            setDevError(err instanceof Error ? err.message : "Could not save VOD");
        } finally {
            setDevBusy(false);
        }
    }

    async function handleClearVod() {
        if (!currentGame || devBusy) return;
        setVodDraft(emptyVodDraft());
        setDevBusy(true);
        setDevError(null);
        setDevMessage(null);
        try {
            const result = await gamesApi.updateVod(currentGame.id, {
                url: null,
                draftOffsetSeconds: null,
                gameStartOffsetSeconds: null,
            });
            queryClient.setQueryData<MatchDetails>(["match", id], (current) => {
                if (!current) return current;
                return mergeSavedVodIntoMatch(current, currentGame.id, {
                    vodUrl: result.vodUrl,
                    baseUrl: result.baseUrl,
                    draftOffsetSeconds: result.draftOffsetSeconds,
                    gameStartOffsetSeconds: result.gameStartOffsetSeconds,
                });
            });
            setDevMessage("VOD cleared.");
            await queryClient.invalidateQueries({ queryKey: ["match", id] });
            await queryClient.invalidateQueries({ queryKey: ["matches"] });
        } catch (err) {
            setDevError(err instanceof Error ? err.message : "Could not clear VOD");
        } finally {
            setDevBusy(false);
        }
    }

    const devTools =
        import.meta.env.DEV && currentGame
            ? {
                  busy: devBusy,
                  message: devMessage,
                  error: devError,
                  vodDraft,
                  onVodDraftChange: setVodDraft,
                  onImportSeries: handleDevImportSeries,
                  onSaveVod: handleSaveVod,
                  onClearVod: handleClearVod,
              }
            : undefined;

    return (
        <div className="page">
            <div className="match-detail__outer">
                <div className="match-detail">
                    <MatchHero
                        match={match}
                        from={from}
                        backLabel={backLabel}
                        devMenu={devTools ? (
                            <MatchDevMenu
                                currentGame={currentGame}
                                gameCount={playedGames.length}
                                devTools={devTools}
                            />
                        ) : undefined}
                        footer={(
                            <GameHeroFooter
                                games={playedGames}
                                currentGame={currentGame}
                                onSelect={setSelectedGame}
                            />
                        )}
                    />
                    {currentGame && (
                        <GamePanel
                            match={match}
                            currentGame={currentGame}
                            gameDetails={gameDetails}
                            gameLoading={gameLoading}
                            gameDetailsError={gameDetailsError}
                        />
                    )}
                </div>
            </div>
        </div>
    );
}
