import type { DraftEntryDto } from "@/lib/api.ts";

export interface DraftSideBuckets {
    leftBans: DraftEntryDto[];
    rightBans: DraftEntryDto[];
    leftPicks: DraftEntryDto[];
    rightPicks: DraftEntryDto[];
    banOrder: Map<number, number>;
    pickOrder: Map<number, number>;
}

function teamForSide(blueIsTeam1: boolean, side: "left" | "right"): 1 | 2 {
    if (side === "left") return blueIsTeam1 ? 1 : 2;
    return blueIsTeam1 ? 2 : 1;
}

function filterDraft(
    draft: DraftEntryDto[],
    teamNumber: 1 | 2,
    phase: "Ban" | "Pick",
): DraftEntryDto[] {
    return draft
        .filter((entry) => entry.teamNumber === teamNumber && entry.phase === phase)
        .sort((a, b) => a.sequenceNumber - b.sequenceNumber);
}

function buildPhaseOrder(draft: DraftEntryDto[], phase: "Ban" | "Pick"): Map<number, number> {
    return new Map(
        draft
            .filter((entry) => entry.phase === phase)
            .sort((a, b) => a.sequenceNumber - b.sequenceNumber)
            .map((entry, index) => [entry.sequenceNumber, index + 1]),
    );
}

export function resolveDraftSides(draft: DraftEntryDto[], team1Side: string | null): DraftSideBuckets {
    const blueIsTeam1 = team1Side === "Blue";
    const leftTeam = teamForSide(blueIsTeam1, "left");
    const rightTeam = teamForSide(blueIsTeam1, "right");

    return {
        leftBans: filterDraft(draft, leftTeam, "Ban"),
        rightBans: filterDraft(draft, rightTeam, "Ban"),
        leftPicks: filterDraft(draft, leftTeam, "Pick"),
        rightPicks: filterDraft(draft, rightTeam, "Pick"),
        banOrder: buildPhaseOrder(draft, "Ban"),
        pickOrder: buildPhaseOrder(draft, "Pick"),
    };
}
