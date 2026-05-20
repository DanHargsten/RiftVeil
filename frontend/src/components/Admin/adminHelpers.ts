import type { TeamListItem } from "@/lib/api.ts";

/** Short, actionable message from long API error bodies. */
export function formatAdminApiError(message: string): string {
    const shortDup = message.match(/duplicate key value is \(([^)]+)\)/i);
    if (shortDup && message.includes("IX_Teams_ShortName")) {
        return `Duplicate team short name "${shortDup[1]}". Another team already uses it — short name was skipped for conflicting rows. Logos and other fields may still have updated; re-run without overwrite or fix shorts in Teams.`;
    }

    if (message.includes("used in") && message.includes("match")) {
        return message.replace(/^API error:\s*\d+:\s*/, "");
    }

    if (message.includes("Invalid column name")) {
        return "Database schema is out of date. Run EF migrations (IconLogoUrl) and restart the API.";
    }

    const firstLine = message.split("\n")[0]?.trim();
    if (firstLine?.startsWith("API error:")) {
        const inner = firstLine.replace(/^API error:\s*\d+:\s*/, "");
        if (inner.length < 200) return inner;
    }

    return message.length > 280 ? `${message.slice(0, 280)}…` : message;
}

export type TeamDataProblems = {
    total: number;
    missingIconUrl: number;
    missingLogoUrl: number;
    missingShortName: number;
};

export function countTeamDataProblems(
    teams: ReadonlyArray<Pick<TeamListItem, "iconLogoUrl" | "logoUrl" | "shortName">>,
): TeamDataProblems {
    const total = teams.length;
    const missingIconUrl = teams.filter((t) => !t.iconLogoUrl?.trim()).length;
    const missingLogoUrl = teams.filter((t) => !t.logoUrl?.trim()).length;
    const missingShortName = teams.filter((t) => {
        const short = t.shortName?.trim() ?? "";
        return short.length === 0 || short.toUpperCase() === "UNK";
    }).length;
    return { total, missingIconUrl, missingLogoUrl, missingShortName };
}
