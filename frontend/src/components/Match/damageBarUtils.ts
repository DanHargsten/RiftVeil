import type { PlayerStatsDto } from "@/lib/api.ts";
import {
    allocatePlayersByRole,
    isBlueSideFirst,
    type LaneRole,
} from "@/components/Match/laneMatchupUtils.ts";

/** Third column: game-wide or per-team damage scale. */
export type TertiaryDamageView = "game" | "team";

export interface VisualTeam {
    shortName: string;
    players: PlayerStatsDto[];
    playersByRole: Record<LaneRole, PlayerStatsDto | null>;
}

export type DamageViewMode = "lane" | "game" | "team";

export interface DamageScales {
    globalMax: number;
    leftTeamMax: number;
    rightTeamMax: number;
}

function maxDamage(players: PlayerStatsDto[]): number {
    if (players.length === 0) return 1;
    return Math.max(1, ...players.map((p) => p.damageDealtToChampions));
}

export function computeDamageScales(
    team1Players: PlayerStatsDto[],
    team2Players: PlayerStatsDto[],
    team1Side: string | null,
): DamageScales {
    const blueFirst = isBlueSideFirst(team1Side);
    const leftTeam = blueFirst ? team1Players : team2Players;
    const rightTeam = blueFirst ? team2Players : team1Players;

    return {
        globalMax: maxDamage([...team1Players, ...team2Players]),
        leftTeamMax: maxDamage(leftTeam),
        rightTeamMax: maxDamage(rightTeam),
    };
}

export function barWidthPct(dmg: number, scale: number): number {
    if (scale <= 0) return 0;
    return Math.min(100, (dmg / scale) * 100);
}

export function laneSharePct(leftDmg: number, rightDmg: number): { leftPct: number; rightPct: number } {
    const total = leftDmg + rightDmg;
    if (total <= 0) return { leftPct: 50, rightPct: 50 };
    return {
        leftPct: (leftDmg / total) * 100,
        rightPct: (rightDmg / total) * 100,
    };
}

export function barWidthStyle(pct: number): { width: string } {
    return { width: `${pct}%` };
}

export function getVisualTeams(
    team1Players: PlayerStatsDto[],
    team2Players: PlayerStatsDto[],
    team1Side: string | null,
    team1ShortName: string,
    team2ShortName: string,
): { left: VisualTeam; right: VisualTeam } {
    const blueFirst = isBlueSideFirst(team1Side);
    const leftPlayers = blueFirst ? team1Players : team2Players;
    const rightPlayers = blueFirst ? team2Players : team1Players;
    return {
        left: {
            shortName: blueFirst ? team1ShortName : team2ShortName,
            players: leftPlayers,
            playersByRole: allocatePlayersByRole(leftPlayers),
        },
        right: {
            shortName: blueFirst ? team2ShortName : team1ShortName,
            players: rightPlayers,
            playersByRole: allocatePlayersByRole(rightPlayers),
        },
    };
}

export function getScaleForMode(
    mode: DamageViewMode,
    scales: DamageScales,
    side: "left" | "right",
): number {
    if (mode === "game") return scales.globalMax;
    if (mode === "team") return side === "left" ? scales.leftTeamMax : scales.rightTeamMax;
    return 1;
}
