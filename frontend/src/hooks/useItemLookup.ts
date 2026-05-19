import { useQuery } from "@tanstack/react-query";
import { useDdragonVersion } from "@/hooks/useDdragonVersion.ts";

interface ItemData {
    name: string;
    image: { full: string };
}

interface ItemJson {
    data: Record<string, ItemData>;
}

function normalizeItemName(name: string): string {
    return name
        .toLowerCase()
        .replace(/[’']/g, "")
        .replace(/\./g, "")
        .replace(/[^a-z0-9]+/g, " ")
        .trim();
}

// Builds a reverse map: item name → item id
function buildNameToIdMap(data: Record<string, ItemData>): Map<string, string> {
    const map = new Map<string, string>();
    for (const [id, item] of Object.entries(data)) {
        map.set(item.name.toLowerCase(), id);
        map.set(normalizeItemName(item.name), id);
    }
    return map;
}

export function useItemLookup() {
    const { version: ddragonVersion, fallbackVersion } = useDdragonVersion();
    const itemJsonUrl = ddragonVersion
        ? `https://ddragon.leagueoflegends.com/cdn/${ddragonVersion}/data/en_US/item.json`
        : null;
    const { data: nameToId } = useQuery({
        queryKey: ["ddragon-items", ddragonVersion],
        queryFn: async () => {
            if (!itemJsonUrl || !ddragonVersion) {
                return new Map<string, string>();
            }
            const res = await fetch(itemJsonUrl);
            if (!res.ok) throw new Error("Failed to fetch item data");
            const json: ItemJson = await res.json();
            return buildNameToIdMap(json.data);
        },
        enabled: !!ddragonVersion && !!itemJsonUrl,
        staleTime: Infinity, // Item data doesn't change during a session
        gcTime: Infinity,
    });

    const getItemId = (name: string): string | null => {
        if (!nameToId) return null;
        return nameToId.get(name.toLowerCase())
            ?? nameToId.get(normalizeItemName(name))
            ?? null;
    };

    const getItemIconUrl = (name: string): string | null => {
        if (!ddragonVersion) return null;
        const id = getItemId(name);
        if (!id) return null;
        return `https://ddragon.leagueoflegends.com/cdn/${ddragonVersion}/img/item/${id}.png`;
    };

    return {
        getItemIconUrl,
        getItemId,
        isLoaded: !!nameToId && !!ddragonVersion,
        ddragonVersion: ddragonVersion ?? fallbackVersion,
        hasResolvedVersion: !!ddragonVersion,
    };
}
