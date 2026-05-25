export const TBD_SHORT = "TBD";
export const TBD_FULL = "To Be Decided";

export function isTbdTeam(shortName: string | null | undefined): boolean {
    return shortName?.trim().toUpperCase() === TBD_SHORT;
}

function tbdFullDisplayName(fullName: string): string {
    const normalized = fullName.trim();
    if (normalized.length > 0 && normalized.toUpperCase() !== TBD_SHORT) {
        return normalized;
    }

    return TBD_FULL;
}

export function formatTeamDisplayNames(
    shortName: string,
    fullName: string,
): { short: string; full: string } {
    if (!isTbdTeam(shortName)) {
        return { short: shortName, full: fullName };
    }

    return { short: TBD_SHORT, full: tbdFullDisplayName(fullName) };
}
