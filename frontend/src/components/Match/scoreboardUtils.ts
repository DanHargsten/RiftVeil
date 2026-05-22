import type { PlayerStatsDto } from "@/lib/api.ts";

export interface ItemSlot {
    name: string;
    isTrinket: boolean;
}

export function formatKdaRatio(player: PlayerStatsDto): string {
    return player.deaths === 0
        ? "Perfect"
        : ((player.kills + player.assists) / player.deaths).toFixed(1);
}

export function buildItemSlots(player: PlayerStatsDto, side: "left" | "right"): ItemSlot[] {
    const items = player.itemIds ? player.itemIds.split(";").filter(Boolean) : [];
    const trinket = player.trinketId ?? null;

    if (side === "right") {
        return [
            ...(trinket ? [{ name: trinket, isTrinket: true }] : []),
            ...items.slice().reverse().map((name) => ({ name, isTrinket: false })),
        ];
    }

    return [
        ...items.map((name) => ({ name, isTrinket: false })),
        ...(trinket ? [{ name: trinket, isTrinket: true }] : []),
    ];
}

export function scoreboardClass(base: string, side?: "left" | "right", extra = ""): string {
    const sidePart = side ? ` ${base}--${side}` : "";
    const extraPart = extra ? ` ${extra}` : "";
    return `${base}${sidePart}${extraPart}`.trim();
}
