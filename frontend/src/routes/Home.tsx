import { useQuery } from "@tanstack/react-query";
import { matchesApi, type MatchListItem } from "@/lib/api.ts";
import { Link } from "react-router-dom";
import { TeamLogo } from "@/components/common/TeamLogo.tsx";

/** Home page: hero + live matchers + upcoming + recent result. */
export function Home() {
    const { data: liveMatches } = useQuery({
        queryKey: ["matches", "live"],
        queryFn: () => matchesApi.getLive(),
        refetchInterval: 30_000,
    });
    
    const { data: upcomingMatches } = useQuery({
        queryKey: ["matches", "upcoming"],
        queryFn: () => matchesApi.getUpcoming(3),
    });
    
    const { data: recentMatches } = useQuery({
        queryKey: ["matches", "recent"],
        queryFn: () => matchesApi.getRecent(8),
    });
    
    const hasLive = liveMatches && liveMatches.length > 0;
    const hasUpcoming = upcomingMatches && upcomingMatches.length > 0;
    const hasRecent = recentMatches && recentMatches.length > 0;
    
    return (
        <div className="page home">
            {/* Hero */}
            <section className="home__hero">
                <h1 className="home__title">Rift Veil</h1>
                <p className="home__tagline">
                    Spoiler-free League of Legends match tracker and VODs
                </p>
            </section>
            
            {/* Live matches */}
            {hasLive && (
                <section className="home__section">
                    <div className="home__section-header">
                        <h2 className="home__section-title">
                            <span className="home__live-dot" />
                            Live now
                        </h2>
                    </div>
                    <div className="home__match-grid">
                        {liveMatches.map((match) => (
                            <HomeMatchCard key={match.id} match={match} />
                        ))}
                    </div>
                </section>                
            )}
            
            {/* Upcoming matches */}
            {hasUpcoming && (
                <section className="home__section">
                    <div className="home__section-header">
                        <h2 className="home__section-title">Upcoming</h2>
                        <Link to="/matches" className="home__section-link">
                            View all matches
                        </Link>
                    </div>
                    <div className="home__match-grid">
                        {upcomingMatches.slice(0, 6).map((match) => (
                            <HomeMatchCard key={match.id} match={match} />
                        ))}
                    </div>
                </section>
            )}
            
            {/* Recent results */}
            {hasRecent && (
                <section className="home__section">
                    <div className="home__section-header">
                        <h2 className="home__section-title">Recent results</h2>
                        <Link to="/matches" className="home__section-link">
                            View all matches
                        </Link>                              
                    </div>
                    <div className="home__match-grid">
                        {recentMatches.map((match) => (
                            <HomeMatchCard key={match.id} match={match} />
                        ))}
                    </div>
                </section>            
            )}
            
            {/* Empty state */}
            {!hasLive && !hasUpcoming && !hasRecent && (
                <section className="home__section">
                    <p className="home__empty">No matches available right now</p>
                </section>
            )}
        </div>
    );
}

/** Compact match card for the home page. */
function HomeMatchCard({match}: { match: MatchListItem; }) {
    const isFinished = match.status === "Finished";
    const isLive = match.status === "Live";
    
    return (
        <Link
            to={`/matches/${match.id}`}
            className={`home-match ${isLive ? "home-match--live" : ""} ${isFinished ? "home-match__finished" : ""}`}
        >
            {/* League + time header */}
            <div className="home-match__header">
                <span className="home-match__league">{match.leagueShortName}</span>
                {isLive ? (
                    <span className="home-match__live-badge">LIVE</span>
                ) : (
                    <time className="home-match__time" dateTime={match.startsAtUtc}>
                        {formatMatchTime(match.startsAtUtc, isFinished)}
                    </time>
                )}
            </div>
            
            {/* Teams */}
            <div className="home-match__body">
                <div className="home-match__team">
                    <TeamLogo shortName={match.team1ShortName} size={28} />
                    <span className="home-match__team-name">{match.team1ShortName}</span>
                </div>
                
                <div className="home-match__center">
                    {isFinished && match.team1Score != null && match.team2Score != null ? (
                        <span className="home-match__score">
                            <span className={match.team1Score > match.team2Score ? "home-match__score--winner" : ""}>
                                {match.team1Score}
                            </span>
                            <span className="home-match__score-sep">&ndash</span>
                            <span className={match.team2Score > match.team1Score ? "home-match__score--winner" : ""}>
                                {match.team2Score}
                            </span>
                        </span>
                    ) : (
                        <span className="home-match__score">vs</span>
                    )}
                </div>
                
                <div className="home-match__team home-match__team--right">
                    <span className="home-match__team-name">{match.team2ShortName}</span>
                    <TeamLogo shortName={match.team2ShortName} size={28} />
                </div>
            </div>
            
            {/* Best of */}
            <div className="home-match__footer">
                <span className="home-match__bo">Bo{match.bestOf}</span>
                {match.round && (
                    <span className="home-match__round">{match.round}</span>
                )}
            </div>
        </Link>
    );
}

function formatMatchTime(isoUtc: string, isFinished: boolean): string {
    const date = new Date(isoUtc);
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    const matchDay = new Date(date.getFullYear(), date.getMonth(), date.getDate());
    const diffDays = Math.round((matchDay.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
    
    const time = date.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" });
    
    if (isFinished) {
        if (diffDays === 0) {
            return "Today";
        }
        if (diffDays === -1) {
            return "Yesterday";
        }
        return date.toLocaleDateString(undefined, { month: "short", day: "numeric" });
    }
    
    if (diffDays === 0) {
        return time;
    }
    if (diffDays === 1) {
        return `Tomorrow ${time}`;
    }
    return date.toLocaleDateString(undefined, { weekday: "short", month: "short", day: "numeric" });
}