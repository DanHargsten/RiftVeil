import { resolveDraftSides } from "@/components/Match/draftUtils.ts";
import type { DraftEntryDto } from "@/lib/api.ts";

interface GameDraftProps {
    draft: DraftEntryDto[];
    team1Side: string | null;
}

export function GameDraft({ draft, team1Side }: GameDraftProps) {
    if (draft.length === 0) {
        return <p className="draft__empty">Draft data not available for this game.</p>;
    }

    const sides = resolveDraftSides(draft, team1Side);

    return (
        <section className="draft" aria-label="Champion draft">
            <div className="draft__body">
                <DraftSide
                    align="left"
                    bans={sides.leftBans}
                    picks={sides.leftPicks}
                    banOrder={sides.banOrder}
                    pickOrder={sides.pickOrder}
                />
                <div className="draft__separator" aria-hidden="true" />
                <DraftSide
                    align="right"
                    bans={sides.rightBans}
                    picks={sides.rightPicks}
                    banOrder={sides.banOrder}
                    pickOrder={sides.pickOrder}
                />
            </div>
        </section>
    );
}

function DraftSide({
    align,
    bans,
    picks,
    banOrder,
    pickOrder,
}: {
    align: "left" | "right";
    bans: DraftEntryDto[];
    picks: DraftEntryDto[];
    banOrder: Map<number, number>;
    pickOrder: Map<number, number>;
}) {
    return (
        <div className={`draft__${align}`}>
            <div className={`draft__bans draft__bans--${align}`}>
                <div className="draft__bans-icons">
                    {bans.map((entry) => (
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
            <div className={`draft__pick-icons draft__pick-icons--${align}`}>
                {picks.map((entry) => (
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
    const phaseClass = phase.toLowerCase();

    return (
        <div
            className={`draft__champ-icon draft__champ-icon--${phaseClass}`}
            style={{ width: size, height: size }}
            title={champion}
        >
            <img
                src={url}
                alt={champion}
                width={size}
                height={size}
                onError={(event) => {
                    (event.target as HTMLImageElement).style.opacity = "0";
                }}
            />
            {phase === "Pick" ? <span className="draft__champ-seq">{sequenceNumber}</span> : null}
        </div>
    );
}
