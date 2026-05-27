function normalizeChampionKey(value: string): string {
    return value.toLowerCase().replace(/[^a-z0-9]/g, "");
}

function cleanChampionId(value: string): string {
    return value.replace(/[^a-zA-Z0-9]/g, "");
}

function toSentenceCase(value: string): string {
    if (!value) {
        return value;
    }

    return value.charAt(0).toUpperCase() + value.slice(1).toLowerCase();
}

// Data Dragon champion file names use mixed casing for some ids (e.g. KogMaw, RekSai).
// Keep overrides normalized so inputs from different sources still resolve correctly.
const CHAMPION_ID_OVERRIDES: Record<string, string> = {
    belveth: "Belveth",
    chogath: "Chogath",
    drmundo: "DrMundo",
    jarvaniv: "JarvanIV",
    khazix: "Khazix",
    kogmaw: "KogMaw",
    monkeyking: "MonkeyKing",
    wukong: "MonkeyKing",
    nunu: "Nunu",
    nunuwillump: "Nunu",
    reksai: "RekSai",
    renata: "Renata",
    renataglasc: "Renata",
    tahmkench: "TahmKench",
    velkoz: "Velkoz",
    xinzhao: "XinZhao",
};

export function buildChampionIdCandidates(champion: string): string[] {
    const cleaned = cleanChampionId(champion);
    const normalizedKey = normalizeChampionKey(champion);
    const override = CHAMPION_ID_OVERRIDES[normalizedKey];
    const sentenceCased = toSentenceCase(cleaned);

    const candidates = [override, cleaned, sentenceCased].filter(
        (candidate): candidate is string => !!candidate && candidate.length > 0,
    );

    return Array.from(new Set(candidates));
}

export function buildChampionIconUrls(champion: string, ddragonVersion: string): string[] {
    return buildChampionIdCandidates(champion).map(
        (championId) =>
            `https://ddragon.leagueoflegends.com/cdn/${ddragonVersion}/img/champion/${championId}.png`,
    );
}
