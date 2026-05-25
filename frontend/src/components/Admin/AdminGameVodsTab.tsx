import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useId, useState } from "react";
import { formatAdminApiError } from "@/components/Admin/adminHelpers.ts";
import {
    formatVodTimestamp,
    isVodOffsetConfigured,
    parseVodTimestamp,
    vodTimestampFieldError,
} from "@/components/Match/vodTimestampUtils.ts";
import { stripPlaybackOffset } from "@/components/Match/vodPlaybackUtils.ts";
import { gamesApi, matchesApi, type GameListItem, type MatchDetails } from "@/lib/api.ts";

type GameVodDraft = {
    url: string;
    draftOffsetSeconds: string;
    gameStartOffsetSeconds: string;
};

function draftFromGame(game: GameListItem): GameVodDraft {
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

function GameVodRow({
    game,
    matchLabel,
    onSaved,
}: {
    game: GameListItem;
    matchLabel: string;
    onSaved: (message: string) => void;
}) {
    const [draft, setDraft] = useState<GameVodDraft>(() => draftFromGame(game));
    const [rowError, setRowError] = useState<string | null>(null);

    const saveMutation = useMutation({
        mutationFn: (body: {
            url: string | null;
            draftOffsetSeconds: number | null;
            gameStartOffsetSeconds: number | null;
        }) => gamesApi.updateVod(game.id, body),
        onSuccess: (result) => {
            setRowError(null);
            setDraft({
                url: result.baseUrl ?? result.vodUrl ?? "",
                draftOffsetSeconds: isVodOffsetConfigured(result.draftOffsetSeconds)
                    ? formatVodTimestamp(result.draftOffsetSeconds)
                    : "",
                gameStartOffsetSeconds: isVodOffsetConfigured(result.gameStartOffsetSeconds)
                    ? formatVodTimestamp(result.gameStartOffsetSeconds)
                    : "",
            });
            onSaved(`Game ${game.gameNumber} VOD saved.`);
        },
        onError: (error: Error) => {
            setRowError(formatAdminApiError(error.message));
        },
    });

    function handleSave() {
        const draftOffsetSeconds = parseVodTimestamp(draft.draftOffsetSeconds);
        const gameStartOffsetSeconds = parseVodTimestamp(draft.gameStartOffsetSeconds);
        if (draftOffsetSeconds === undefined) {
            setRowError(vodTimestampFieldError("Draft start"));
            return;
        }
        if (gameStartOffsetSeconds === undefined) {
            setRowError(vodTimestampFieldError("Game start"));
            return;
        }

        const url = draft.url.trim();
        saveMutation.mutate({
            url: url.length > 0 ? url : null,
            draftOffsetSeconds,
            gameStartOffsetSeconds,
        });
    }

    return (
        <tr>
            <td>{game.gameNumber}</td>
            <td className="admin__vods-cell-id">{game.id}</td>
            <td>
                {game.vodUrl ? (
                    <a href={game.vodUrl} target="_blank" rel="noopener noreferrer" className="admin__vods-link">
                        Open current
                    </a>
                ) : (
                    <span className="admin__vods-empty">None</span>
                )}
            </td>
            <td>
                <label className="sr-only" htmlFor={`${matchLabel}-game-${game.id}-url`}>
                    YouTube or Twitch URL for game {game.gameNumber}
                </label>
                <input
                    id={`${matchLabel}-game-${game.id}-url`}
                    type="url"
                    className="admin__input admin__input--cell admin__vods-input-url"
                    placeholder="https://www.youtube.com/watch?v=…"
                    value={draft.url}
                    onChange={(event) => setDraft((prev) => ({ ...prev, url: event.target.value }))}
                    disabled={saveMutation.isPending}
                />
            </td>
            <td>
                <label className="sr-only" htmlFor={`${matchLabel}-game-${game.id}-draft-offset`}>
                    Draft start timestamp for game {game.gameNumber}
                </label>
                <input
                    id={`${matchLabel}-game-${game.id}-draft-offset`}
                    type="text"
                    inputMode="numeric"
                    className="admin__input admin__input--cell admin__vods-input-offset"
                    placeholder="0"
                    autoComplete="off"
                    spellCheck={false}
                    value={draft.draftOffsetSeconds}
                    onChange={(event) => setDraft((prev) => ({ ...prev, draftOffsetSeconds: event.target.value }))}
                    disabled={saveMutation.isPending}
                />
            </td>
            <td>
                <label className="sr-only" htmlFor={`${matchLabel}-game-${game.id}-game-offset`}>
                    Game start timestamp for game {game.gameNumber}
                </label>
                <input
                    id={`${matchLabel}-game-${game.id}-game-offset`}
                    type="text"
                    inputMode="numeric"
                    className="admin__input admin__input--cell admin__vods-input-offset"
                    placeholder="6:21"
                    autoComplete="off"
                    spellCheck={false}
                    value={draft.gameStartOffsetSeconds}
                    onChange={(event) => setDraft((prev) => ({ ...prev, gameStartOffsetSeconds: event.target.value }))}
                    disabled={saveMutation.isPending}
                />
            </td>
            <td>
                <button
                    type="button"
                    className="admin__mini-btn"
                    onClick={handleSave}
                    disabled={saveMutation.isPending}
                >
                    {saveMutation.isPending ? "Saving…" : "Save"}
                </button>
                {rowError ? (
                    <p className="admin__warning admin__vods-row-error" role="alert">{rowError}</p>
                ) : null}
            </td>
        </tr>
    );
}

export function AdminGameVodsTab() {
    const ids = useId();
    const matchIdFieldId = `${ids}-match-id`;
    const queryClient = useQueryClient();
    const [matchIdInput, setMatchIdInput] = useState("");
    const [loadedMatchId, setLoadedMatchId] = useState<number | null>(null);
    const [feedback, setFeedback] = useState<string | null>(null);
    const [loadError, setLoadError] = useState<string | null>(null);

    const { data: match, isLoading, isFetching, error } = useQuery({
        queryKey: ["admin-match-vods", loadedMatchId],
        queryFn: () => matchesApi.getById(loadedMatchId!),
        enabled: loadedMatchId != null,
    });

    function handleLoadMatch(event: React.FormEvent) {
        event.preventDefault();
        setFeedback(null);
        setLoadError(null);

        const parsed = Number.parseInt(matchIdInput.trim(), 10);
        if (!Number.isFinite(parsed) || parsed <= 0) {
            setLoadError("Enter a valid match ID (positive number).");
            setLoadedMatchId(null);
            return;
        }

        setLoadedMatchId(parsed);
    }

    function handleSaved(message: string) {
        setFeedback(message);
        if (loadedMatchId != null) {
            queryClient.invalidateQueries({ queryKey: ["admin-match-vods", loadedMatchId] });
            queryClient.invalidateQueries({ queryKey: ["matches"] });
        }
    }

    const queryError = error instanceof Error ? formatAdminApiError(error.message) : null;

    return (
        <section className="admin__section admin__section--wide" aria-labelledby="game-vods-heading">
            <h2 id="game-vods-heading" className="admin__section-title">Game VODs</h2>

            <div className="admin__form admin__vods-form">
                <p className="admin__hint">
                    Add third-party VOD links (e.g. Onivia highlights) when LoLesports import has nothing,
                    or to point at a full broadcast URL. Timestamps from the video player (e.g. 1:03 or 6:21).
                </p>

                <form className="admin__vods-load" onSubmit={handleLoadMatch}>
                    <label className="admin__label" htmlFor={matchIdFieldId}>Match ID</label>
                    <div className="admin__vods-load-row">
                        <input
                            id={matchIdFieldId}
                            type="number"
                            min={1}
                            className="admin__input admin__vods-match-id"
                            value={matchIdInput}
                            onChange={(event) => setMatchIdInput(event.target.value)}
                            placeholder="e.g. 42"
                        />
                        <button type="submit" className="admin__run-btn" disabled={isFetching}>
                            {isFetching ? "Loading…" : "Load match"}
                        </button>
                    </div>
                </form>

                {loadError ? <p className="admin__warning" role="alert">{loadError}</p> : null}
                {queryError ? <p className="admin__warning" role="alert">{queryError}</p> : null}
                {feedback ? <p className="admin__hint" role="status">{feedback}</p> : null}

                {isLoading && loadedMatchId != null ? (
                    <p className="admin__placeholder">Loading match…</p>
                ) : null}

                {match ? (
                    <MatchVodEditor match={match} onSaved={handleSaved} />
                ) : null}
            </div>
        </section>
    );
}

function MatchVodEditor({
    match,
    onSaved,
}: {
    match: MatchDetails;
    onSaved: (message: string) => void;
}) {
    const playedGames = [...match.games].sort((a, b) => a.gameNumber - b.gameNumber);
    const matchLabel = `match-${match.id}`;

    return (
        <div className="admin__vods-match">
            <p className="admin__vods-match-title">
                <strong>{match.team1ShortName}</strong> vs <strong>{match.team2ShortName}</strong>
                {" · "}
                {match.tournamentName}
                {match.round ? ` · ${match.round}` : ""}
            </p>

            {playedGames.length === 0 ? (
                <p className="admin__placeholder">This match has no games yet.</p>
            ) : (
                <div className="admin__teams-table-wrap">
                    <table className="admin__teams-table admin__vods-table">
                        <thead>
                            <tr>
                                <th scope="col">Game</th>
                                <th scope="col">Game ID</th>
                                <th scope="col">Current</th>
                                <th scope="col">URL</th>
                                <th scope="col">Draft start</th>
                                <th scope="col">Game start</th>
                                <th scope="col">Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            {playedGames.map((game) => (
                                <GameVodRow
                                    key={`${game.id}-${game.vodUrl ?? "none"}`}
                                    game={game}
                                    matchLabel={matchLabel}
                                    onSaved={onSaved}
                                />
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}
