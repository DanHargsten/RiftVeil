const TEAM_PLACEHOLDER = "/logos/teams/placeholder.png";

export type TeamLogoVariant = "icon" | "full";

function normalizeShort(shortName: string): string {
    return shortName.trim().toLowerCase();
}

/** Local square/isotype asset (e.g. `t1-square.png`). */
export function localTeamIconPath(shortName: string): string {
    return `/logos/teams/${normalizeShort(shortName)}-square.png`;
}

/** Local wordmark/full asset (e.g. `t1.png`). */
export function localTeamWordmarkPath(shortName: string): string {
    return `/logos/teams/${normalizeShort(shortName)}.png`;
}

/** @deprecated Use localTeamWordmarkPath or localTeamIconPath */
export function localTeamLogoPath(shortName: string): string {
    return localTeamWordmarkPath(shortName);
}

function resolutionChain(
    logoUrl: string | null | undefined,
    iconLogoUrl: string | null | undefined,
    shortName: string,
    variant: TeamLogoVariant,
): string[] {
    if (variant === "icon") {
        const chain = [
            localTeamIconPath(shortName),
            localTeamWordmarkPath(shortName),
        ];
        const remote = iconLogoUrl?.trim();
        if (remote) chain.push(remote);
        chain.push(TEAM_PLACEHOLDER);
        return chain;
    }

    const fullChain = [localTeamWordmarkPath(shortName)];
    const remoteLogo = logoUrl?.trim();
    if (remoteLogo) fullChain.push(remoteLogo);
    fullChain.push(TEAM_PLACEHOLDER);
    return fullChain;
}

/**
 * Local file first, then API URL, then placeholder.
 * Icon: `{short}-square.png` → `{short}.png` → IconLogoUrl → placeholder.
 */
export function resolveTeamLogoSrc(
    logoUrl: string | null | undefined,
    iconLogoUrl: string | null | undefined,
    shortName: string,
    variant: TeamLogoVariant = "icon",
): string {
    return resolutionChain(logoUrl, iconLogoUrl, shortName, variant)[0];
}

export function teamLogoFallbackSrc(
    currentSrc: string,
    shortName: string,
    logoUrl?: string | null,
    iconLogoUrl?: string | null,
    variant: TeamLogoVariant = "icon",
): string {
    const chain = resolutionChain(logoUrl, iconLogoUrl, shortName, variant);
    if (currentSrc === TEAM_PLACEHOLDER) return currentSrc;

    const index = chain.indexOf(currentSrc);
    if (index >= 0 && index < chain.length - 1) {
        return chain[index + 1];
    }

    if (currentSrc.startsWith("http")) {
        for (let i = chain.length - 1; i >= 0; i--) {
            if (chain[i].startsWith("http") && chain[i] === currentSrc && i < chain.length - 1) {
                return chain[i + 1];
            }
        }
        return TEAM_PLACEHOLDER;
    }

    const localIndex = chain.findIndex(
        (path) => path !== TEAM_PLACEHOLDER && !path.startsWith("http")
            && (currentSrc === path || currentSrc.endsWith(path)),
    );
    if (localIndex >= 0 && localIndex < chain.length - 1) {
        return chain[localIndex + 1];
    }

    return TEAM_PLACEHOLDER;
}
