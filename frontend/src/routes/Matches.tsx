import { MatchList } from "@/components/MatchList";
import { TournamentSidebar } from "@/components/TournamentSidebar";
import { useState } from "react";

// Match page: list all matches with spoiler protection
export function Matches() {
    const [selectedTournamentId, setSelectedTournamentId] = useState<number | null>(null);

    return (
        <div className="page page--with-sidebar">
            <TournamentSidebar
                selectedTournamentId={selectedTournamentId}
                onSelect={setSelectedTournamentId}
            />
            <div className="page__content">
                <MatchList tournamentId={selectedTournamentId} />
            </div>
        </div>
    );
}
