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

function objectivesBodyClass(team1Side: string | null | undefined): string {
    if (team1Side == null) {
        return "match-detail__objectives-body";
    }
    return isBlueSideFirst(team1Side)
        ? "match-detail__objectives-body match-detail__objectives-body--blue-t1"
        : "match-detail__objectives-body match-detail__objectives-body--red-t1";
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

    return (
        <div
            className={objectivesBodyClass(gameDetails.team1Side)}
            role="group"
            aria-labelledby="match-detail-objectives-title"
        >
            <div className="match-detail__objectives-head">
                <span className="match-detail__objectives-head-score">{match.team1ShortName}</span>
                <span className="match-detail__objectives-head-mid" aria-hidden="true" />
                <span className="match-detail__objectives-head-score">{match.team2ShortName}</span>
            </div>
            <div className="match-detail__objectives-list">
                {OBJECTIVE_ROWS.map((row) => (
                    <ObjectiveRow
                        key={row.field}
                        label={row.label}
                        field={row.field}
                        team1Name={match.team1ShortName}
                        team2Name={match.team2ShortName}
                        team1Value={statValue(t1, row.field)}
                        team2Value={statValue(t2, row.field)}
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
    team1Name,
    team2Name,
    team1Value,
    team2Value,
}: {
    label: string;
    field: ObjectiveField;
    team1Name: string;
    team2Name: string;
    team1Value: number;
    team2Value: number;
}) {
    const ariaLabel = `${label}: ${team1Name} ${team1Value}, ${team2Name} ${team2Value}`;

    return (
        <div className="match-detail__objectives-row" aria-label={ariaLabel}>
            <div className="match-detail__objectives-score match-detail__objectives-score--t1">
                {team1Value}
            </div>
            <div className="match-detail__objectives-mid">
                <ObjectiveRowIcon field={field} />
                <span className="match-detail__objectives-label">{label}</span>
            </div>
            <div className="match-detail__objectives-score match-detail__objectives-score--t2">
                {team2Value}
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
