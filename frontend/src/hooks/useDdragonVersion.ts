import { useQuery } from "@tanstack/react-query";

const FALLBACK_VERSION = "15.8.1";
const VERSION_URL = "https://ddragon.leagueoflegends.com/api/versions.json";

export function useDdragonVersion() {
    const { data, isLoading } = useQuery({
        queryKey: ["ddragon-version-latest"],
        queryFn: async () => {
            const res = await fetch(VERSION_URL);
            if (!res.ok) throw new Error("Failed to fetch ddragon versions");
            const versions: string[] = await res.json();
            const latest = versions[0] ?? FALLBACK_VERSION;
            return latest;
        },
        staleTime: Infinity,
        gcTime: Infinity,
    });

    return {
        version: data ?? null,
        isLoading,
        fallbackVersion: FALLBACK_VERSION,
    };
}

