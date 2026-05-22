import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { GameHeroFooter } from "@/components/Match/GameHeroFooter.tsx";
import { GamePanel } from "@/components/Match/GamePanel.tsx";
import { MatchHero } from "@/components/Match/MatchHero.tsx";
import { gamesApi, matchesApi } from "@/lib/api.ts";

/** Match detail page: full match info with game-by-game breakdown. */
export function MatchDetail() {
    const { id } = useParams<{ id: string }>();
    const location = useLocation();
    const queryClient = useQueryClient();
    const [selectedGame, setSelectedGame] = useState<number>(1);
    const [devImportBusy, setDevImportBusy] = useState(false);
    const [devImportError, setDevImportError] = useState<string | null>(null);

    // Back link target: optional `state.from` set by MatchCard
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
        setDevImportError(null);
    }, [currentGame?.id]);

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

    async function handleDevImportGameDetails() {
        if (!currentGame || devImportBusy) return;
        setDevImportBusy(true);
        setDevImportError(null);
        const gameId = currentGame.id;
        try {
            const res = await fetch(`/api/import/game-details/game/${gameId}`, { method: "POST" });
            const text = (await res.text()).trim();
            if (!res.ok) {
                setDevImportError(text || `Import failed (${res.status})`);
                return;
            }
            await queryClient.invalidateQueries({ queryKey: ["game-details", gameId] });
        } catch (err) {
            setDevImportError(err instanceof Error ? err.message : "Import failed");
        } finally {
            setDevImportBusy(false);
        }
    }

    const devImport =
        import.meta.env.DEV && currentGame
            ? {
                  busy: devImportBusy,
                  error: devImportError,
                  onImport: handleDevImportGameDetails,
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
                        footer={(
                            <GameHeroFooter
                                games={playedGames}
                                currentGame={currentGame}
                                onSelect={setSelectedGame}
                                devImport={devImport}
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
