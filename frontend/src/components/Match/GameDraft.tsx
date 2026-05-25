import { resolveDraftSides } from "@/components/Match/draftUtils.ts";
import statGoldIcon from "@/assets/icons/lol-icons/lol-stat-gold.png";
import statKdaIcon from "@/assets/icons/lol-icons/lol-stat-kda.png";
import type { DraftEntryDto } from "@/lib/api.ts";

export interface DraftTeamStats {
    shortName: string;
    kda: string;
    gold: string;
}

interface GameDraftProps {
    draft: DraftEntryDto[];
    team1Side: string | null;
    leftTeam: DraftTeamStats;
    rightTeam: DraftTeamStats;
}

export function GameDraft({ draft, team1Side, leftTeam, rightTeam }: GameDraftProps) {
    if (draft.length === 0) {
        return <p className="draft__empty">Draft data not available for this game.</p>;
    }

    const sides = resolveDraftSides(draft, team1Side);

    return (
        <section className="draft" aria-label="Champion draft">
            <div className="draft__body">
                <DraftTeamStatsColumn align="left" teamStats={leftTeam} />
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
                <DraftTeamStatsColumn align="right" teamStats={rightTeam} />
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

function DraftTeamStatsColumn({
    align,
    teamStats,
}: {
    align: "left" | "right";
    teamStats: DraftTeamStats;
}) {
    const { shortName, kda, gold } = teamStats;
    const mirrored = align === "right";

    return (
        <div className={`draft__team-stats draft__team-stats--${align}`}>
            <DraftTeamStat
                type="kda"
                value={kda}
                shortName={shortName}
                mirrored={mirrored}
            />
            <DraftTeamStat
                type="gold"
                value={gold}
                shortName={shortName}
                mirrored={mirrored}
            />
        </div>
    );
}

function DraftTeamStat({
    type,
    value,
    shortName,
    mirrored,
}: {
    type: "kda" | "gold";
    value: string;
    shortName: string;
    mirrored: boolean;
}) {
    const icon = type === "kda" ? statKdaIcon : statGoldIcon;
    const label = type === "kda" ? "kills, deaths and assists" : "total gold";
    const statClass = `draft__team-stat draft__team-stat--${type}${mirrored ? " draft__team-stat--mirrored" : ""}`;

    return (
        <span className={statClass} aria-label={`${shortName} ${label}`}>
            <img src={icon} alt="" aria-hidden="true" className="draft__team-stat-icon" />
            <span>{value}</span>
        </span>
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
