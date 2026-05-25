import type { ReactNode } from "react";
import baronIcon from "@/assets/icons/lol-objectives/baron.png";
import dragonIcon from "@/assets/icons/lol-objectives/dragon.png";
import towerIcon from "@/assets/icons/lol-objectives/tower.png";
import { isBlueSideFirst } from "@/components/Match/laneMatchupUtils.ts";
import type { GameDetailsDto, MatchDetails, TeamStatsDto } from "@/lib/api.ts";

export interface GameObjectivesProps {
    match: MatchDetails;
    gameDetails: GameDetailsDto | undefined;
    loading: boolean;
    error: boolean;
}

type ObjectiveField = "towersDestroyed" | "totalDragonsSlain" | "baronsSlain" | "voidGrubsSlain";

const OBJECTIVE_ROWS: { label: string; field: ObjectiveField }[] = [
    { label: "Towers", field: "towersDestroyed" },
    { label: "Dragons", field: "totalDragonsSlain" },
    { label: "Barons", field: "baronsSlain" },
    { label: "Void grubs", field: "voidGrubsSlain" },
];

function statValue(stats: TeamStatsDto | null | undefined, field: ObjectiveField): number {
    return stats?.[field] ?? 0;
}

function resolveObjectiveSides(
    match: MatchDetails,
    gameDetails: GameDetailsDto,
): {
    leftName: string;
    rightName: string;
    leftStats: TeamStatsDto | null | undefined;
    rightStats: TeamStatsDto | null | undefined;
} {
    const blueFirst = isBlueSideFirst(gameDetails.team1Side);

    return {
        leftName: blueFirst ? match.team1ShortName : match.team2ShortName,
        rightName: blueFirst ? match.team2ShortName : match.team1ShortName,
        leftStats: blueFirst ? gameDetails.team1Stats : gameDetails.team2Stats,
        rightStats: blueFirst ? gameDetails.team2Stats : gameDetails.team1Stats,
    };
}

export function GameObjectives({ match, gameDetails, loading, error }: GameObjectivesProps) {
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

    const sides = resolveObjectiveSides(match, gameDetails);

    return (
        <div
            className="match-detail__objectives-body"
            role="group"
            aria-labelledby="match-detail-objectives-title"
        >
            <div className="match-detail__objectives-head">
                <span className="match-detail__objectives-head-score">{sides.leftName}</span>
                <span className="match-detail__objectives-head-mid" aria-hidden="true" />
                <span className="match-detail__objectives-head-score">{sides.rightName}</span>
            </div>
            <div className="match-detail__objectives-list">
                {OBJECTIVE_ROWS.map((row) => (
                    <ObjectiveRow
                        key={row.field}
                        label={row.label}
                        field={row.field}
                        leftName={sides.leftName}
                        rightName={sides.rightName}
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
    leftName,
    rightName,
    leftValue,
    rightValue,
}: {
    label: string;
    field: ObjectiveField;
    leftName: string;
    rightName: string;
    leftValue: number;
    rightValue: number;
}) {
    const ariaLabel = `${label}: ${leftName} ${leftValue}, ${rightName} ${rightValue}`;

    return (
        <div className="match-detail__objectives-row" aria-label={ariaLabel}>
            <div className="match-detail__objectives-score match-detail__objectives-score--left">
                {leftValue}
            </div>
            <div className="match-detail__objectives-mid">
                <ObjectiveRowIcon field={field} />
                <span className="match-detail__objectives-label">{label}</span>
            </div>
            <div className="match-detail__objectives-score match-detail__objectives-score--right">
                {rightValue}
            </div>
        </div>
    );
}

function ObjectiveRowIcon({ field }: { field: ObjectiveField }) {
    if (field === "voidGrubsSlain") {
        return (
            <svg
                className="match-detail__objectives-icon match-detail__objectives-icon--svg"
                viewBox="0 0 20 12"
                aria-hidden
            >
                <ellipse cx="5" cy="6" rx="3.2" ry="4.2" fill="currentColor" />
                <ellipse cx="10" cy="6" rx="3.2" ry="4.2" fill="currentColor" />
                <ellipse cx="15" cy="6" rx="3.2" ry="4.2" fill="currentColor" />
            </svg>
        );
    }

    const src = field === "towersDestroyed" ? towerIcon : field === "totalDragonsSlain" ? dragonIcon : baronIcon;

    return (
        <img
            src={src}
            alt=""
            className="match-detail__objectives-icon match-detail__objectives-icon--img"
            width={20}
            height={20}
            aria-hidden
        />
    );
}
