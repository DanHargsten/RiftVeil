import { Link } from "react-router-dom";
import { LeagueLogo, TeamLogo } from "@/components/common/Logos.tsx";
import type { MatchDetails } from "@/lib/api.ts";

interface MatchHeroProps {
    match: MatchDetails;
    from: string;
    backLabel: string;
}

export function MatchHero({ match, from, backLabel }: MatchHeroProps) {
    const team1Wins = match.team1Score ?? 0;
    const team2Wins = match.team2Score ?? 0;
    const team1IsWinner = team1Wins > team2Wins;
    const team2IsWinner = team2Wins > team1Wins;
    const leagueShortName = match.tournament.leagueShortName;
    const tournamentStage = match.tournament.stage;

    return (
        <header className="match-detail__hero">
            <h1 className="sr-only">
                Match: {match.team1Name} vs {match.team2Name}
                {tournamentStage ? ` — ${tournamentStage}` : ""}
            </h1>

            <div className="match-detail__breadcrumb">
                <Link to={from} className="match-detail__back-link">
                    {backLabel}
                </Link>
                <div className="match-detail__league-info">
                    <LeagueLogo shortName={leagueShortName} className="match-detail__league-logo" />
                    <span className="match-detail__league-name">{leagueShortName}</span>
                    {tournamentStage && (
                        <>
                            <span className="match-detail__meta-sep">·</span>
                            <span className="match-detail__tournament-stage">{tournamentStage}</span>
                        </>
                    )}
                    <span className="match-detail__meta-sep">·</span>
                    <span className="match-detail__best-of">Best of {match.bestOf}</span>
                </div>
            </div>

            {/* Teams + Score */}
            <div className="match-detail__scoreline">

                {/* Team 1 */}
                <div
                    className={`match-detail__team match-detail__team--left ${team1IsWinner ? "match-detail__team--winner" : team2IsWinner ? "match-detail__team--loser" : ""}`}>
                    <div className="match-detail__team-identity">
                        <span className="match-detail__team-short">{match.team1ShortName}</span>
                        <span className="match-detail__team-full">{match.team1Name}</span>
                    </div>
                    <div className="match-detail__team-logo-wrap">
                        <TeamLogo shortName={match.team1ShortName} className="match-detail__team-logo" />
                    </div>
                </div>

                {/* Score */}
                <div className="match-detail__score-block">
                    <div className="match-detail__score">
                        <span
                            className={`match-detail__score-num ${team1IsWinner ? "match-detail__score-num--winner" : ""}`}>
                            {team1Wins}
                        </span>
                        <span className="match-detail__score-divider">–</span>
                        <span
                            className={`match-detail__score-num ${team2IsWinner ? "match-detail__score-num--winner" : ""}`}>
                            {team2Wins}
                        </span>
                    </div>
                    {(team1IsWinner || team2IsWinner) && (
                        <span className="match-detail__winner-label">
                            {team1IsWinner ? match.team1ShortName : match.team2ShortName} wins
                        </span>
                    )}
                </div>

                {/* Team 2 */}
                <div
                    className={`match-detail__team match-detail__team--right ${team2IsWinner ? "match-detail__team--winner" : team1IsWinner ? "match-detail__team--loser" : ""}`}>
                    <div className="match-detail__team-logo-wrap">
                        <TeamLogo shortName={match.team2ShortName} className="match-detail__team-logo" />
                    </div>
                    <div className="match-detail__team-identity">
                        <span className="match-detail__team-short">{match.team2ShortName}</span>
                        <span className="match-detail__team-full">{match.team2Name}</span>
                    </div>
                </div>
            </div>
        </header>
    );
}