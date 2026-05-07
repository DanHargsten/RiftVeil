import { useEffect, useState } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { GamePanel } from "@/components/Match/GamePanel.tsx";
import { GameTabs } from "@/components/Match/GameTabs.tsx";
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
            <div className="page">
                <div className="match-detail-loading">
                    <div className="match-detail-loading__spinner" />
                    <span>Loading match...</span>
                </div>
            </div>
        );
    }

    if (error || !match) {
        return (
            <div className="page">
                <div className="match-detail-error">
                    <span>Match not found.</span>
                    <Link to={from} className="match-detail-error__back">← Back</Link>
                </div>
            </div>
        );
    }

    const getGameWinnerShort = (winningTeam: number | null) => {
        if (winningTeam === 1) return match.team1ShortName;
        if (winningTeam === 2) return match.team2ShortName;
        return null;
    };

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
                
                <aside className="match-detail__sidebar match-detail__sidebar--left">
                    <section className="match-detail__section">
                        <h2 className="match-detail__section-title">Objectives</h2>
                        <div className="match-detail__placeholder-body">
                            <span>Coming soon</span>
                        </div>
                    </section>
                </aside>
                
                <div className="match-detail">                    
                    <MatchHero
                        match={match}
                        from={from}
                        backLabel={backLabel}
                    />                    
                    <GameTabs
                        games={playedGames}
                        currentGame={currentGame}
                        onSelect={setSelectedGame}
                        getWinnerShort={getGameWinnerShort}
                    />
                    {currentGame && (
                        <GamePanel
                            match={match}
                            currentGame={currentGame}
                            gameDetails={gameDetails}
                            gameLoading={gameLoading}
                            gameDetailsError={gameDetailsError}
                            devImport={devImport}
                        />
                    )}
                </div>

                <aside className="match-detail__sidebar match-detail__sidebar--right">
                    <section className="match-detail__section">
                        <h2 className="match-detail__section-title">Highlights</h2>
                        <div className="match-detail__placeholder-body">
                            <span>Coming soon</span>
                        </div>
                    </section>
                </aside>
            </div>
        </div>
    );
}