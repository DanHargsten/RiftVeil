import type { GameDetailsDto, PlayerStatsDto } from "@/lib/api.ts";

export function sumPlayerStat(
    players: PlayerStatsDto[],
    key: "kills" | "deaths" | "assists" | "goldEarned",
): number {
    return players.reduce((sum, player) => sum + player[key], 0);
}

export function formatTeamKda(players: PlayerStatsDto[]): string {
    return `${sumPlayerStat(players, "kills")}/${sumPlayerStat(players, "deaths")}/${sumPlayerStat(players, "assists")}`;
}

export function formatGoldStat(gold: number): string {
    return gold >= 1000 ? `${(gold / 1000).toFixed(1)}k` : String(gold);
}

export function formatGameDurationLabel(gameDetails: GameDetailsDto): string | null {
    const seconds =
        gameDetails.team1Stats?.gameDurationSeconds
        ?? gameDetails.team2Stats?.gameDurationSeconds
        ?? gameDetails.gameDurationSeconds
        ?? null;
    return seconds != null && seconds > 0 ? formatDuration(seconds) : null;
}

export function formatDuration(seconds: number): string {
    const minutes = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${minutes}:${secs.toString().padStart(2, "0")}`;
}

export function teamOutcomeClass(isWinner: boolean, opponentWon: boolean): string {
    if (isWinner) return "match-detail__team--winner";
    if (opponentWon) return "match-detail__team--loser";
    return "";
}

export function scoreOutcomeClass(isWinner: boolean): string {
    return isWinner ? "match-detail__score-num--winner" : "";
}

export function modeButtonClass(isActive: boolean): string {
    return `damage-bars__mode-btn${isActive ? " damage-bars__mode-btn--active" : ""}`;
}

export function tabButtonClass(isActive: boolean): string {
    return `match-detail__tab${isActive ? " match-detail__tab--active" : ""}`;
}
