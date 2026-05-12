import type { CSSProperties } from "react";

import baronIcon from "@/assets/icons/lol-objectives/baron.png";
import dragonIcon from "@/assets/icons/lol-objectives/dragon.png";
import towerIcon from "@/assets/icons/lol-objectives/tower.png";
import type { GameDetailsDto, MatchDetails, TeamStatsDto } from "@/lib/api.ts";

export interface GameObjectivesProps {
    match: MatchDetails;
    gameDetails: GameDetailsDto | undefined;
    loading: boolean;
    error: boolean;
}

type ObjectiveField = "towersDestroyed" | "totalDragonsSlain" | "baronsSlain" | "voidGrubsSlain";

const LOL_BLUE = "#5ba3ff";
const LOL_RED = "#f05858";
const LOL_ORANGE = "#ff9f52";

/** Map side → score colours (left column = team 1, right = team 2). */
function objectiveScoreColorVars(team1Side: string | null | undefined): CSSProperties {
    if (!team1Side) {
        return {
            "--objective-score-t1": LOL_BLUE,
            "--objective-score-t2": LOL_ORANGE,
        } as CSSProperties;
    }
    const blueFirst = team1Side.toLowerCase() === "blue";
    return {
        "--objective-score-t1": blueFirst ? LOL_BLUE : LOL_RED,
        "--objective-score-t2": blueFirst ? LOL_RED : LOL_BLUE,
    } as CSSProperties;
}

function statValue(stats: TeamStatsDto | null | undefined, field: ObjectiveField): number {
    return stats?.[field] ?? 0;
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
        <span
            className="match-detail__objectives-icon match-detail__objectives-icon--masked"
            style={{ "--objective-icon-src": `url(${src})` } as CSSProperties}
            aria-hidden
        />
    );
}

export function GameObjectives({ match, gameDetails, loading, error }: GameObjectivesProps) {
    if (loading) {
        return (
            <div className="match-detail__objectives-body match-detail__objectives-body--state">
                <div className="match-detail-loading__spinner" aria-hidden="true" />
                <span>Loading objectives…</span>
            </div>
        );
    }

    if (error) {
        return (
            <div className="match-detail__objectives-body match-detail__objectives-body--state" role="alert">
                <span>Could not load objectives.</span>
            </div>
        );
    }

    if (!gameDetails) {
        return (
            <div className="match-detail__objectives-body match-detail__objectives-body--state">
                <span className="match-detail__objectives-muted">No game selected.</span>
            </div>
        );
    }

    const t1 = gameDetails.team1Stats;
    const t2 = gameDetails.team2Stats;

    if (t1 == null && t2 == null) {
        return (
            <div className="match-detail__objectives-body match-detail__objectives-body--state">
                <span className="match-detail__objectives-muted">No objective data for this game.</span>
            </div>
        );
    }

    const rows: { label: string; field: ObjectiveField }[] = [
        { label: "Towers", field: "towersDestroyed" },
        { label: "Dragons", field: "totalDragonsSlain" },
        { label: "Barons", field: "baronsSlain" },
        { label: "Void grubs", field: "voidGrubsSlain" },
    ];

    const scoreColors = objectiveScoreColorVars(gameDetails.team1Side);

    return (
        <div
            className="match-detail__objectives-body"
            role="group"
            aria-labelledby="match-detail-objectives-title"
            style={scoreColors}
        >
            <div className="match-detail__objectives-head">
                <span className="match-detail__objectives-head-score">{match.team1ShortName}</span>
                <span className="match-detail__objectives-head-mid" aria-hidden="true" />
                <span className="match-detail__objectives-head-score">{match.team2ShortName}</span>
            </div>
            <div className="match-detail__objectives-list">
                {rows.map(({ label, field }) => {
                    const v1 = statValue(t1, field);
                    const v2 = statValue(t2, field);
                    return (
                        <div
                            key={field}
                            className="match-detail__objectives-row"
                            aria-label={`${label}: ${match.team1ShortName} ${v1}, ${match.team2ShortName} ${v2}`}
                        >
                            <div className="match-detail__objectives-score match-detail__objectives-score--t1">
                                {v1}
                            </div>
                            <div className="match-detail__objectives-mid">
                                <ObjectiveRowIcon field={field} />
                                <span className="match-detail__objectives-label">{label}</span>
                            </div>
                            <div className="match-detail__objectives-score match-detail__objectives-score--t2">
                                {v2}
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
}
