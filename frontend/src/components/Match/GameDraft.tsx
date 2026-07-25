import { useMemo, useState } from "react";
import { resolveDraftSides } from "@/components/Match/draftUtils.ts";
import { buildChampionIconUrls } from "@/components/Match/championIconUtils.ts";
import { useDdragonVersion } from "@/hooks/useDdragonVersion.ts";
import type { DraftEntryDto } from "@/lib/api.ts";

interface GameDraftProps {
    draft: DraftEntryDto[];
    team1Side: string | null;
}

export function GameDraft({ draft, team1Side }: GameDraftProps) {
    const { version, fallbackVersion } = useDdragonVersion();
    const ddragonVersion = version ?? fallbackVersion;

    if (draft.length === 0) {
        return <p className="draft__empty">Draft data not available for this game.</p>;
    }

    const sides = resolveDraftSides(draft, team1Side);
    return (
        <section className="draft" aria-label="Champion draft">
            <div className="draft__body">
                {/* ========== LEFT TEAM ========== */}
                <DraftSide
                    align="left"
                    bans={sides.leftBans}
                    picks={sides.leftPicks}
                    banOrder={sides.banOrder}
                    pickOrder={sides.pickOrder}
                    ddragonVersion={ddragonVersion}
                />

                {/* ========== CENTER SEPARATOR ========== */}
                <div className="draft__separator" aria-hidden="true" />

                {/* ========== RIGHT TEAM ========== */}
                <DraftSide
                    align="right"
                    bans={sides.rightBans}
                    picks={sides.rightPicks}
                    banOrder={sides.banOrder}
                    pickOrder={sides.pickOrder}
                    ddragonVersion={ddragonVersion}
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
    ddragonVersion,
}: {
    align: "left" | "right";
    bans: DraftEntryDto[];
    picks: DraftEntryDto[];
    banOrder: Map<number, number>;
    pickOrder: Map<number, number>;
    ddragonVersion: string;
}) {
    return (
        <div className={`draft__${align}`}>
            <div className={`draft__bans draft__bans--${align}`}>
                <div className="draft__bans-icons">
                    {bans.map((entry) => (
                        <DraftChampIcon
                            key={`${entry.sequenceNumber}-${entry.champion}-${ddragonVersion}`}
                            champion={entry.champion}
                            phase="Ban"
                            size={40}
                            sequenceNumber={banOrder.get(entry.sequenceNumber)}
                            ddragonVersion={ddragonVersion}
                        />
                    ))}
                </div>
            </div>
            <div className={`draft__pick-icons draft__pick-icons--${align}`}>
                {picks.map((entry) => (
                    <DraftChampIcon
                        key={`${entry.sequenceNumber}-${entry.champion}-${ddragonVersion}`}
                        champion={entry.champion}
                        phase="Pick"
                        size={50}
                        sequenceNumber={pickOrder.get(entry.sequenceNumber)}
                        ddragonVersion={ddragonVersion}
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
    ddragonVersion,
}: {
    champion: string;
    phase: "Pick" | "Ban";
    size: number;
    sequenceNumber?: number;
    ddragonVersion: string;
}) {
    const [candidateIndex, setCandidateIndex] = useState(0);
    const [hasError, setHasError] = useState(false);
    const urls = useMemo(
        () => buildChampionIconUrls(champion, ddragonVersion),
        [champion, ddragonVersion],
    );
    const currentUrl = urls[candidateIndex] ?? null;
    const phaseClass = phase.toLowerCase();

    return (
        <div
            className={`draft__champ-icon draft__champ-icon--${phaseClass}`}
            style={{ width: size, height: size }}
            title={champion}
            role="img"
            aria-label={`${phase} ${champion}`}
        >
            {hasError || !currentUrl ? null : (
                <img
                    src={currentUrl}
                    alt=""
                    aria-hidden="true"
                    width={size}
                    height={size}
                    onError={() => {
                        if (candidateIndex < urls.length - 1) {
                            setCandidateIndex((current) => current + 1);
                            return;
                        }

                        setHasError(true);
                    }}
                />
            )}
            {phase === "Pick" ? <span className="draft__champ-seq" aria-hidden="true">{sequenceNumber}</span> : null}
        </div>
    );
}
