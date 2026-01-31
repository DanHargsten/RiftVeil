import {matchesApi, type MatchListItem} from "@/lib/api.ts";
import {useQuery} from "@tanstack/react-query";

export function MatchList() {
    const { data: matches, isLoading, error } = useQuery({
        queryKey: ["matches", "upcoming"],
        queryFn: () => matchesApi.getUpcoming(7),
    });
    
    if (isLoading) {
        return <div className="loading">Loading matches...</div>
    }
    
    if (error) {
      return (
          <div className="error">
              Error loading matches: {error.message}
          </div>
      );
    }
    
    if (!matches || matches.length === 0) {
        return (
            <div className="empty">
                No upcoming matches in the next 7 days.
            </div>
        );
    }
    
    return (
        <div className="match-list">
            <h2 className="match-list__title">Upcoming Matches</h2>
            
            <div className="match-list__items">
                {matches.map((match) => (
                    <MatchCard key={match.id} match={match} />
                ))}
            </div>
        </div>
    );
}

function MatchCard({ match }: { match: MatchListItem }) {
    const matchDate = new Date(match.startsAtUtc);
    
    const dateString = matchDate.toLocaleDateString("sv-SE", {
        month: "short",
        day: "numeric"
    });

    const timeString = matchDate.toLocaleTimeString("sv-SE", {
        hour: "2-digit",
        minute: "2-digit"
    });
    
    return (
        <article className="match-card">
            <div className="match-card__content">
                <div className="match-card__teams">
                    <span className="match-card__team">{match.team1Name}</span>
                    <span className="match-card__vs">vs</span>
                    <span className="match-card__team">{match.team2Name}</span>
                </div>
                
                <div className="match-card__meta">
                    {match.tournamentName} • Bo{match.bestOf}
                </div>
            </div>
            
            <div className="match-card__info">
                <time className="match-card__date" dateTime={match.startsAtUtc}>
                    {dateString}
                </time>
                <time className="match-card__time" dateTime={timeString}>
                    {timeString}
                </time>
            </div>    
            
            <StatusBadge status={match.status} />
        </article>
    );
}

function StatusBadge({ status }: { status: MatchListItem["status"] }) {
    return (
        <span className={`status-badge status-badge--${status.toLowerCase()}`}>
            {status}
        </span>
    )
}