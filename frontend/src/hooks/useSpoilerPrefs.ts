import { useState, useEffect } from "react";

export type SpoilerPrefs = {
    globalEnabled: boolean;
    revealedMatchIds: Set<number>;
};

const STORAGE_KEY = "spoiler-prefs";

function loadFromStorage(): SpoilerPrefs {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (!raw) return { globalEnabled: true, revealedMatchIds: new Set() };
        const parsed = JSON.parse(raw);
        return {
            globalEnabled: parsed.globalEnabled ?? false,
            revealedMatchIds: new Set(parsed.revealedMatchIds ?? []),
        };
    } catch {
        return { globalEnabled: false, revealedMatchIds: new Set() };
    }
}

function saveToStorage(prefs: SpoilerPrefs) {
    localStorage.setItem(
        STORAGE_KEY,
        JSON.stringify({
            globalEnabled: prefs.globalEnabled,
            revealedMatchIds: Array.from(prefs.revealedMatchIds),
        })
    );
}

export function useSpoilerPrefs() {
    const [spoilers, setSpoilers] = useState<SpoilerPrefs>(loadFromStorage);

    useEffect(() => {
        saveToStorage(spoilers);
    }, [spoilers]);

    const toggleGlobal = () =>
        setSpoilers((prev) => ({ ...prev, globalEnabled: !prev.globalEnabled }));

    const revealMatch = (id: number) =>
        setSpoilers((prev) => {
            const next = new Set(prev.revealedMatchIds);
            next.add(id);
            return { ...prev, revealedMatchIds: next };
        });

    const hideMatch = (id: number) =>
        setSpoilers((prev) => {
            const next = new Set(prev.revealedMatchIds);
            next.delete(id);
            return { ...prev, revealedMatchIds: next };
        });

    return { spoilers, toggleGlobal, revealMatch, hideMatch };
}