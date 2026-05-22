import type { PlayerStatsDto } from "@/lib/api.ts";
import roleBotIcon from "@/assets/icons/lol-icons/role-bot.png";
import roleJungleIcon from "@/assets/icons/lol-icons/role-jungle.png";
import roleMidIcon from "@/assets/icons/lol-icons/role-mid.png";
import roleSupportIcon from "@/assets/icons/lol-icons/role-support.png";
import roleTopIcon from "@/assets/icons/lol-icons/role-top.png";

export type LaneRole = "top" | "jungle" | "mid" | "bot" | "support";

export const ROLE_ORDER: LaneRole[] = ["top", "jungle", "mid", "bot", "support"];

export const ROLE_META: Record<LaneRole, { label: string; icon: string }> = {
    top: { label: "Top", icon: roleTopIcon },
    jungle: { label: "Jungle", icon: roleJungleIcon },
    mid: { label: "Mid", icon: roleMidIcon },
    bot: { label: "Bot", icon: roleBotIcon },
    support: { label: "Support", icon: roleSupportIcon },
};

export interface LaneMatchupRow {
    role: LaneRole;
    leftPlayer: PlayerStatsDto | null;
    rightPlayer: PlayerStatsDto | null;
}

export function isBlueSideFirst(team1Side: string | null | undefined): boolean {
    return team1Side == null || team1Side.toLowerCase() === "blue";
}

export function buildLaneMatchupRows(
    team1Players: PlayerStatsDto[],
    team2Players: PlayerStatsDto[],
    team1Side: string | null,
): LaneMatchupRow[] {
    const blueFirst = isBlueSideFirst(team1Side);
    const leftPlayersByRole = allocatePlayersByRole(blueFirst ? team1Players : team2Players);
    const rightPlayersByRole = allocatePlayersByRole(blueFirst ? team2Players : team1Players);
    return ROLE_ORDER.map((role) => ({
        role,
        leftPlayer: leftPlayersByRole[role],
        rightPlayer: rightPlayersByRole[role],
    }));
}

export function allocatePlayersByRole(players: PlayerStatsDto[]): Record<LaneRole, PlayerStatsDto | null> {
    const byRole: Record<LaneRole, PlayerStatsDto | null> = {
        top: null,
        jungle: null,
        mid: null,
        bot: null,
        support: null,
    };
    const leftovers: PlayerStatsDto[] = [];

    for (const player of players) {
        const role = normalizeRole(player.ingameRole);
        if (!role || byRole[role] != null) {
            leftovers.push(player);
            continue;
        }
        byRole[role] = player;
    }

    for (const role of ROLE_ORDER) {
        if (byRole[role] == null && leftovers.length > 0) {
            byRole[role] = leftovers.shift() ?? null;
        }
    }

    return byRole;
}

export function normalizeRole(rawRole: string | null | undefined): LaneRole | null {
    if (!rawRole) return null;
    const role = rawRole.toLowerCase().trim();
    if (["top", "toplane", "top lane"].includes(role)) return "top";
    if (["jungle", "jungler", "jgl"].includes(role)) return "jungle";
    if (["mid", "middle", "midlane", "mid lane"].includes(role)) return "mid";
    if (["bot", "bottom", "adc", "carry", "bottom lane", "bot lane"].includes(role)) return "bot";
    if (["support", "sup", "supp"].includes(role)) return "support";
    return null;
}

export function formatDamage(dmg: number): string {
    if (dmg >= 1000) return `${(dmg / 1000).toFixed(1)}k`;
    return String(dmg);
}

export function formatPlayerName(name: string): string {
    return name.replace(/\s*\(.*?\)/, "").trim();
}
