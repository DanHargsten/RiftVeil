import type { DraftEntryDto } from "@/lib/api.ts";

interface GameDraftProps {
    draft: DraftEntryDto[];
    team1Side: string | null;
}

export function GameDraft({
    draft,
    team1Side,
}: GameDraftProps) {
    const blueIsTeam1 = team1Side === "Blue";

    const leftBans = draft
        .filter(d => d.teamNumber === (blueIsTeam1 ? 1 : 2) && d.phase === "Ban")
        .sort((a, b) => a.sequenceNumber - b.sequenceNumber);
    const rightBans = draft
        .filter(d => d.teamNumber === (blueIsTeam1 ? 2 : 1) && d.phase === "Ban")
        .sort((a, b) => a.sequenceNumber - b.sequenceNumber);
    const leftPicks = draft
        .filter(d => d.teamNumber === (blueIsTeam1 ? 1 : 2) && d.phase === "Pick")
        .sort((a, b) => a.sequenceNumber - b.sequenceNumber);
    const rightPicks = draft
        .filter(d => d.teamNumber === (blueIsTeam1 ? 2 : 1) && d.phase === "Pick")
        .sort((a, b) => a.sequenceNumber - b.sequenceNumber);

    const banOrder = new Map(
        [...draft]
            .filter(entry => entry.phase === "Ban")
            .sort((a, b) => a.sequenceNumber - b.sequenceNumber)
            .map((entry, i) => [entry.sequenceNumber, i + 1])
    );

    const pickOrder = new Map(
        [...draft]
            .filter(d => d.phase === "Pick")
            .sort((a, b) => a.sequenceNumber - b.sequenceNumber)
            .map((entry, i) => [entry.sequenceNumber, i + 1])
    );
    
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
                            {leftBans.map(entry => (
                                <DraftChampIcon
                                    key={entry.sequenceNumber}
                                    champion={entry.champion}
                                    phase="Ban"
                                    size={40}
                                    sequenceNumber={banOrder.get(entry.sequenceNumber)}
                                />
                            ))}
                        </div>
                    </div>
                    <div className="draft__pick-icons draft__pick-icons--left">
                        {[...leftPicks].map(entry => (
                            <DraftChampIcon
                                key={entry.sequenceNumber}
                                champion={entry.champion}
                                phase="Pick"
                                size={50}
                                sequenceNumber={pickOrder.get(entry.sequenceNumber)}
                            />
                        ))}
                    </div>
                </div>

                <div className="draft__separator" aria-hidden="true" />

                <div className="draft__right">
                    <div className="draft__bans draft__bans--right">
                        <div className="draft__bans-icons">
                            {rightBans.map(entry => (
                                <DraftChampIcon
                                    key={entry.sequenceNumber}
                                    champion={entry.champion}
                                    phase="Ban"
                                    size={40}
                                    sequenceNumber={banOrder.get(entry.sequenceNumber)}
                                />
                            ))}
                        </div>
                    </div>
                    <div className="draft__pick-icons draft__pick-icons--right">
                        {rightPicks.map(entry => (
                            <DraftChampIcon
                                key={entry.sequenceNumber}
                                champion={entry.champion}
                                phase="Pick"
                                size={50}
                                sequenceNumber={pickOrder.get(entry.sequenceNumber)}
                            />
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
    sequenceNumber,
}: {
    champion: string;
    phase: "Pick" | "Ban";
    size: number;
    sequenceNumber?: number;
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
            {phase === "Pick" && (
                <span className="draft__champ-seq">{sequenceNumber}</span>
            )}
        </div>
    );
}

