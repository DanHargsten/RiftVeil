import { MatchList } from "@/components/MatchList";
import { TournamentSidebar } from "@/components/TournamentSidebar";
import { useSpoilerPrefs } from "@/hooks/useSpoilerPrefs.ts";
import { useState } from "react";

/** Matches page: list all matches with spoiler protection. */
export function Matches() {
    const [selectedTournamentId, setSelectedTournamentId] = useState<number | null>(null);
    const { spoilers, toggleGlobal, revealMatch, hideMatch } = useSpoilerPrefs();

    return (
        <div className="page page--with-sidebar">
            <TournamentSidebar
                selectedTournamentId={selectedTournamentId}
                onSelect={setSelectedTournamentId}
            />
            <div className="page__content">
                <MatchList
                    tournamentId={selectedTournamentId}
                    spoilerProps={{ spoilers, toggleGlobal, revealMatch, hideMatch }}
                />
            </div>
        </div>
    );
}
