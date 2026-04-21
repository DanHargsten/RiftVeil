import type { DraftEntryDto } from "@/lib/api.ts";

interface GameDraftProps {
    draft: DraftEntryDto[];
}

export function GameDraft({
    draft,
}: GameDraftProps) {
    // Blue side = Team1 in Leaguepedia's PickAndBansS7,
    // but teamNumber already maps to team1/team2 in the DB
    const team1Bans = draft
        .filter((entry) => entry.teamNumber === 1 && entry.phase === "Ban")
        .sort((a, b) => a.sequenceNumber - b.sequenceNumber);
    const team2Bans = draft
        .filter((entry) => entry.teamNumber === 2 && entry.phase === "Ban")
        .sort((a, b) => a.sequenceNumber - b.sequenceNumber);
    const team1Picks = draft
        .filter((entry) => entry.teamNumber === 1 && entry.phase === "Pick")
        .sort((a, b) => a.sequenceNumber - b.sequenceNumber);
    const team2Picks = draft
        .filter((entry) => entry.teamNumber === 2 && entry.phase === "Pick")
        .sort((a, b) => a.sequenceNumber - b.sequenceNumber);
        
    if (draft.length === 0) {
        return (
            <p className="draft__empty">Draft data not available for this game.</p>
        );
    }

    return (
        <section className="draft" aria-label="Champion draft">
            {/* Bans and picks with center separator */}
            <div className="draft__body">
                <div className="draft__left">
                    <div className="draft__bans draft__bans--left">
                        <div className="draft__bans-icons">
                            {team1Bans.map(entry => (
                                <DraftChampIcon key={entry.sequenceNumber} champion={entry.champion} phase="Ban" size={38} />
                            ))}
                        </div>
                    </div>
                    <div className="draft__pick-icons draft__pick-icons--left">
                        {[...team1Picks].reverse().map(entry => (
                            <DraftChampIcon key={entry.sequenceNumber} champion={entry.champion} phase="Pick" size={48} />
                        ))}
                    </div>
                </div>

                <div className="draft__separator" aria-hidden="true" />

                <div className="draft__right">
                    <div className="draft__bans draft__bans--right">
                        <div className="draft__bans-icons">
                            {team2Bans.map(entry => (
                                <DraftChampIcon key={entry.sequenceNumber} champion={entry.champion} phase="Ban" size={38} />
                            ))}
                        </div>
                    </div>
                    <div className="draft__pick-icons draft__pick-icons--right">
                        {team2Picks.map(entry => (
                            <DraftChampIcon key={entry.sequenceNumber} champion={entry.champion} phase="Pick" size={48} />
                        ))}
                    </div>
                </div>
            </div>
        </section>
    );
}

function DraftChampIcon({
    champion,
    phase,
    size,
}: {
    champion: string;
    phase: "Pick" | "Ban";
    size: number;
}) {
    const normalized = champion.replace(/[^a-zA-Z0-9]/g, "");
    const url = `https://ddragon.leagueoflegends.com/cdn/15.8.1/img/champion/${normalized}.png`;

    return (
        <div
            className={`draft__champ-icon draft__champ-icon--${phase.toLowerCase()}`}
            style={{ width: size, height: size }}
            title={champion}
        >
            <img
                src={url}
                alt={champion}
                width={size}
                height={size}
                onError={e => {
                    (e.target as HTMLImageElement).style.opacity = "0";
                }}
            />
        </div>
    );
}