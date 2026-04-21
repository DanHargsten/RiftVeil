import { useQuery } from "@tanstack/react-query";

const DDRAGON_VERSION = "15.8.1";
const ITEM_JSON_URL = `https://ddragon.leagueoflegends.com/cdn/${DDRAGON_VERSION}/data/en_US/item.json`;

interface ItemData {
    name: string;
    image: { full: string };
}

interface ItemJson {
    data: Record<string, ItemData>;
}

// Builds a reverse map: item name → item id
function buildNameToIdMap(data: Record<string, ItemData>): Map<string, string> {
    const map = new Map<string, string>();
    for (const [id, item] of Object.entries(data)) {
        map.set(item.name.toLowerCase(), id);
    }
    return map;
}

export function useItemLookup() {
    const { data: nameToId } = useQuery({
        queryKey: ["ddragon-items"],
        queryFn: async () => {
            const res = await fetch(ITEM_JSON_URL);
            if (!res.ok) throw new Error("Failed to fetch item data");
            const json: ItemJson = await res.json();
            return buildNameToIdMap(json.data);
        },
        staleTime: Infinity, // Item data doesn't change during a session
        gcTime: Infinity,
    });

    const getItemId = (name: string): string | null => {
        if (!nameToId) return null;
        return nameToId.get(name.toLowerCase()) ?? null;
    };

    const getItemIconUrl = (name: string): string | null => {
        const id = getItemId(name);
        if (!id) return null;
        return `https://ddragon.leagueoflegends.com/cdn/${DDRAGON_VERSION}/img/item/${id}.png`;
    };

    return { getItemIconUrl, getItemId, isLoaded: !!nameToId };
}