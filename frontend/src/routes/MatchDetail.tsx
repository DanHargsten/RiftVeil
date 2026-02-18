import {useParams} from "react-router-dom";
import {useQuery} from "@tanstack/react-query";
import {matchesApi} from "@/lib/api.ts";
import { useState } from "react";
import {TeamLogo} from "@/components/common/TeamLogo.tsx";

/** Match detail page: full match info with game-by-game breakdown. */
export function MatchDetail() {
    const { id } = useParams<{ id: string }>();
    const [selectedGame, setSelectedGame] = useState<number>(1);
    
    const { data: match, isLoading, error } = useQuery({
        queryKey: ["match", id],
        queryFn: () => matchesApi.getById(Number(id)),
    });
    
    if (isLoading) return <div>Loading...</div>
    if (error || !match) return <div>Match not found</div>
    
    const winnerName = match.team1Score != null && match.team2Score != null
        ? match.team1Score > match.team2Score
            ? match.team1ShortName
            : match.team2ShortName
        : null;
    
    const currentGame = match.games.find((g) => g.gameNumber === selectedGame);
    
    return (
        <div className="page">
            <div className="match-detail">
                
                {/* Match header */}
                <div className="match-detail__header">
                    <div className="match-detail__header-body">
                        <div className="match-detail__teams">
                            <div className="match-detail__team">
                                <div className="match-detail__team-logo">
                                    <TeamLogo shortName={match.team1ShortName} className="match-detail__team-logo-img" />                                    
                                </div>
                                
                                <div className="match-detail__team-info">
                                    <span className="match-detail__team-short">{match.team1ShortName}</span>
                                    <span className="match-detail__team-full">{match.team1Name}</span>                                    
                                </div>
                            </div> 
                            
                            <div className="match-detail__score-cell">
                                <span className="match-detail__score">
                                    {match.team1Score} / {match.team2Score}
                                </span>                                
                            </div>

                            <div className="match-detail__team">
                                <div className="match-detail__team-logo">
                                    <TeamLogo shortName={match.team2ShortName} className="match-detail__team-logo-img" />
                                </div>

                                <div className="match-detail__team-info">
                                    <span className="match-detail__team-short">{match.team2ShortName}</span>
                                    <span className="match-detail__team-full">{match.team2Name}</span>
                                </div>
                            </div>
                        </div>
                      
                        {winnerName && (
                            <span className="match-detail__winner">{winnerName} wins</span>
                        )}
                    </div>
                    
                    <div className="match-detail__meta">
                        <span>{match.tournament.name}</span>
                        <span>Best of {match.bestOf}</span>
                    </div>
                </div>                
                
                {/* Game tabs */}
                <div className="match-detail__game-tabs">
                    {match.games.map((game) => {
                        const isActive = selectedGame === game.gameNumber;

                        const winnerShort =
                            game.winningTeam === 1 ? match.team1ShortName :
                            game.winningTeam === 2 ? match.team2ShortName :
                            null;

                        return (
                            <button
                                key={game.id}
                                className={`match-detail__game-tab ${isActive ? "match-detail__game-tab--active" : ""}`}
                                onClick={() => setSelectedGame(game.gameNumber)}
                                type="button"
                            >
                                <span className="match-detail__tab-title">
                                    Game {game.gameNumber}
                                </span>

                                <span className="match-detail__tab-winner">
                                    {winnerShort} win
                                </span>
                            </button>
                        );
                    })}
                </div>

                {/* Selected game content */}
                {currentGame && (
                    <div className="match-detail__game-content">
                        
                        {/* VOD link */}
                        {currentGame.vodUrl && (
                            <a
                                href={currentGame.vodUrl}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="match-detail__vod-link"
                            >
                                Watch VOD
                            </a>
                        )}
                        
                        {/* Draft placeholder */}
                        <div className="match-detail__section">
                            <h2 className="match-detail__section-header">Draft Phase</h2>
                            <p className="match-detail__placeholder">
                                Draft data coming soon
                            </p>
                        </div>
                        
                        {/* Scoreboard placeholder */}
                        <div className="match-detail__section">
                            <h2 className="match-detail__section-header">Scoreboard</h2>
                            <p className="match-detail__placeholder">
                                Player stats coming soon
                            </p>
                        </div>

                        <div className="match-detail__section-grid">
                            {/* Gold graph placeholder */}
                            <div className="match-detail__section">
                                <h2 className="match-detail__section-header">Gold advantage</h2>
                                <p className="match-detail__placeholder">
                                    Gold graph coming soon
                                </p>
                            </div>

                            {/* Objectives placeholder */}
                            <div className="match-detail__section">
                                <h2 className="match-detail__section-header">Objectives</h2>
                                <p className="match-detail__placeholder">
                                    Objectives coming soon
                                </p>
                            </div>
                        </div>
                    </div>
                )}                
            </div>
        </div>
    )
}