import { MatchList } from "@/components/MatchList";
import { TournamentSidebar } from "@/components/TournamentSidebar.tsx";
import { useSpoilerPrefs } from "@/hooks/useSpoilerPrefs.ts";
import { useState } from "react";

/** Home route: match list with spoiler preferences (default tournament window). */
export function Home() {
    const [selectedTournamentId, setSelectedTournamentId] = useState<number | null>(null);
    const spoilerProps = useSpoilerPrefs();

    return (
        <div className="home home--with-sidebar">
            <TournamentSidebar
                selectedTournamentId={selectedTournamentId}
                onSelect={setSelectedTournamentId}
            />
            <section className="home__content">
                <div className="home__content-inner">
                    <MatchList
                        tournamentId={selectedTournamentId}
                        onTournamentChange={setSelectedTournamentId}
                        spoilerProps={spoilerProps}
                    />
                </div>
            </section>
        </div>
    );
}
