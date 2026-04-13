import { useQuery } from "@tanstack/react-query";
import { matchesApi } from "@/lib/api.ts";
import { MatchList } from "@/components/MatchList";
import { useSpoilerPrefs } from "@/hooks/useSpoilerPrefs.ts";

export function Home() {
    const spoilerProps = useSpoilerPrefs();

    const { data: liveMatches } = useQuery({
        queryKey: ["matches", "live"],
        queryFn: () => matchesApi.getLive(),
        refetchInterval: 30_000,
    });

    const { data: upcomingMatches } = useQuery({
        queryKey: ["matches", "upcoming"],
        queryFn: () => matchesApi.getUpcoming(7),
    });

    const liveCount = liveMatches?.length ?? 0;
    const todayCount = upcomingMatches?.filter((m) => {
        const d = new Date(m.startsAtUtc);
        const today = new Date();
        return d.toDateString() === today.toDateString();
    }).length ?? 0;
    const upcomingCount = upcomingMatches?.length ?? 0;

    return (
        <div className="home">
            <div className="home__stats container">
                <div className="home__stat">
                    <span className="home__stat-value home__stat-value--live">{liveCount}</span>
                    <span className="home__stat-label">Live now</span>
                </div>
                <div className="home__stat">
                    <span className="home__stat-value">{todayCount}</span>
                    <span className="home__stat-label">Today</span>
                </div>
                <div className="home__stat">
                    <span className="home__stat-value">{upcomingCount}</span>
                    <span className="home__stat-label">Upcoming</span>
                </div>
            </div>

            <div className="home__content container">
                <MatchList spoilerProps={spoilerProps} />
            </div>
        </div>
    );
}