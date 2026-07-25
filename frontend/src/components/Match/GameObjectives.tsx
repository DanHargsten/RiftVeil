import type { ReactNode } from "react";
import baronIcon from "@/assets/icons/lol-objectives/baron.svg";
import dragonIcon from "@/assets/icons/lol-objectives/dragon.svg";
import heraldIcon from "@/assets/icons/lol-objectives/riftHerald.svg";
import inhibIcon from "@/assets/icons/lol-objectives/inhibitor.svg";
import towerIcon from "@/assets/icons/lol-objectives/tower.svg";
import voidgrubIcon from "@/assets/icons/lol-objectives/voidgrubs.svg";
import { isBlueSideFirst } from "@/components/Match/laneMatchupUtils.ts";
import type { GameDetailsDto, TeamStatsDto } from "@/lib/api.ts";

export interface GameObjectivesProps {
    gameDetails: GameDetailsDto | undefined;
    loading: boolean;
    error: boolean;
}

type ObjectiveField =
    | "voidGrubsSlain"
    | "riftHeraldsSlain"
    | "totalDragonsSlain"
    | "baronsSlain"
    | "towersDestroyed"
    | "inhibitorsDestroyed";

const OBJECTIVE_ROWS: { label: string; field: ObjectiveField }[] = [
    { label: "Voidgrubs", field: "voidGrubsSlain" },
    { label: "Herald", field: "riftHeraldsSlain" },
    { label: "Dragons", field: "totalDragonsSlain" },
    { label: "Barons", field: "baronsSlain" },
    { label: "Towers", field: "towersDestroyed" },
    { label: "Inhibs", field: "inhibitorsDestroyed" },
];

function statValue(stats: TeamStatsDto | null | undefined, field: ObjectiveField): number {
    return stats?.[field] ?? 0;
}

function resolveObjectiveSides(
    gameDetails: GameDetailsDto,
): {
    leftStats: TeamStatsDto | null | undefined;
    rightStats: TeamStatsDto | null | undefined;
} {
    const blueFirst = isBlueSideFirst(gameDetails.team1Side);

    return {
        leftStats: blueFirst ? gameDetails.team1Stats : gameDetails.team2Stats,
        rightStats: blueFirst ? gameDetails.team2Stats : gameDetails.team1Stats,
    };
}

export function GameObjectives({ gameDetails, loading, error }: GameObjectivesProps) {
    if (loading) {
        return (
            <ObjectivesState>
                <div className="match-detail-loading__spinner" aria-hidden="true" />
                <span>Loading objectives…</span>
            </ObjectivesState>
        );
    }

    if (error || !gameDetails) {
        return (
            <ObjectivesState>
                <span className="match-detail__objectives-muted">Could not load objectives.</span>
            </ObjectivesState>
        );
    }

    const t1 = gameDetails.team1Stats;
    const t2 = gameDetails.team2Stats;

    if (t1 == null && t2 == null) {
        return (
            <ObjectivesState>
                <span className="match-detail__objectives-muted">No objective data for this game.</span>
            </ObjectivesState>
        );
    }

    const sides = resolveObjectiveSides(gameDetails);

    return (
        <div
            className="match-detail__objectives-body"
            role="group"
            aria-labelledby="match-detail-objectives-title"
        >
            <div className="match-detail__objectives-list">
                {OBJECTIVE_ROWS.map((row) => (
                    <ObjectiveRow
                        key={row.field}
                        label={row.label}
                        field={row.field}
                        leftValue={statValue(sides.leftStats, row.field)}
                        rightValue={statValue(sides.rightStats, row.field)}
                    />
                ))}
            </div>
        </div>
    );
}

function ObjectivesState({ children }: { children: ReactNode }) {
    return (
        <div className="match-detail__objectives-body match-detail__objectives-body--state">
            {children}
        </div>
    );
}

function ObjectiveRow({
    label,
    field,
    leftValue,
    rightValue,
}: {
    label: string;
    field: ObjectiveField;
    leftValue: number;
    rightValue: number;
}) {
    const ariaLabel = `${label}: left team ${leftValue}, right team ${rightValue}`;

    return (
        <div className="match-detail__objectives-row" aria-label={ariaLabel}>
            <div className="match-detail__objectives-score match-detail__objectives-score--left">
                {leftValue}
            </div>
            <div className="match-detail__objectives-mid">
                <ObjectiveRowIcon field={field} label={label} />
            </div>
            <div className="match-detail__objectives-score match-detail__objectives-score--right">
                {rightValue}
            </div>
        </div>
    );
}

function ObjectiveRowIcon({ field, label }: { field: ObjectiveField; label: string }) {
    const srcByField: Record<ObjectiveField, string> = {
        voidGrubsSlain: voidgrubIcon,
        riftHeraldsSlain: heraldIcon,
        totalDragonsSlain: dragonIcon,
        baronsSlain: baronIcon,
        towersDestroyed: towerIcon,
        inhibitorsDestroyed: inhibIcon,
    };

    return (
        <img
            src={srcByField[field]}
            alt={label}
            title={label}
            className="match-detail__objectives-icon match-detail__objectives-icon--img"
            width={20}
            height={20}
        />
    );
}
