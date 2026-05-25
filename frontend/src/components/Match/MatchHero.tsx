import { Link } from "react-router-dom";
import type { ReactNode } from "react";
import { LeagueLogo, TeamLogo } from "@/components/common/Logos.tsx";
import { scoreOutcomeClass, teamOutcomeClass } from "@/components/Match/matchDisplayUtils.ts";
import { formatTeamDisplayNames } from "@/lib/teamDisplayUtils.ts";
import type { MatchDetails } from "@/lib/api.ts";

interface MatchHeroProps {
    match: MatchDetails;
    from: string;
    backLabel: string;
    footer?: ReactNode;
    devMenu?: ReactNode;
}

export function MatchHero({ match, from, backLabel, footer, devMenu }: MatchHeroProps) {
    const team1Wins = match.team1Score ?? 0;
    const team2Wins = match.team2Score ?? 0;
    const team1IsWinner = team1Wins > team2Wins;
    const team2IsWinner = team2Wins > team1Wins;
    const leagueShortName = match.tournament.leagueShortName;
    const tournamentStage = match.tournament.stage;
    const leaguePath = `/leagues/${leagueShortName.toLowerCase()}`;
    const team1Display = formatTeamDisplayNames(match.team1ShortName, match.team1Name);
    const team2Display = formatTeamDisplayNames(match.team2ShortName, match.team2Name);

    return (
        <header className="match-detail__hero">
            {devMenu ? <div className="match-detail__hero-dev">{devMenu}</div> : null}
            <HeroWatermark
                side="left"
                shortName={match.team1ShortName}
                logoUrl={match.team1LogoUrl}
                iconLogoUrl={match.team1IconLogoUrl}
            />
            <HeroWatermark
                side="right"
                shortName={match.team2ShortName}
                logoUrl={match.team2LogoUrl}
                iconLogoUrl={match.team2IconLogoUrl}
            />

            <h1 className="sr-only">
                Match: {team1Display.full} vs {team2Display.full}
                {tournamentStage ? ` — ${tournamentStage}` : ""}
            </h1>

            <div className="match-detail__breadcrumb">
                <Link to={from} className="match-detail__back-link">
                    {backLabel}
                </Link>
                <span className="match-detail__meta-sep" aria-hidden="true">·</span>
                <div className="match-detail__league-info">
                    <Link to={leaguePath} className="match-detail__league-link">
                        <LeagueLogo shortName={leagueShortName} className="match-detail__league-logo" />
                        <span className="match-detail__league-name">{leagueShortName}</span>
                    </Link>
                    {tournamentStage ? (
                        <>
                            <span className="match-detail__meta-sep" aria-hidden="true">·</span>
                            <span className="match-detail__tournament-stage">{tournamentStage}</span>
                        </>
                    ) : null}
                    <span className="match-detail__meta-sep" aria-hidden="true">·</span>
                    <span className="match-detail__best-of">Best of {match.bestOf}</span>
                </div>
            </div>

            <div className="match-detail__scoreline">
                <HeroTeamBlock
                    align="left"
                    shortName={team1Display.short}
                    fullName={team1Display.full}
                    outcomeClass={teamOutcomeClass(team1IsWinner, team2IsWinner)}
                />
                <div className="match-detail__score-block">
                    <div className="match-detail__score">
                        <span className={`match-detail__score-num ${scoreOutcomeClass(team1IsWinner)}`}>
                            {team1Wins}
                        </span>
                        <span className="match-detail__score-divider">–</span>
                        <span className={`match-detail__score-num ${scoreOutcomeClass(team2IsWinner)}`}>
                            {team2Wins}
                        </span>
                    </div>
                </div>
                <HeroTeamBlock
                    align="right"
                    shortName={team2Display.short}
                    fullName={team2Display.full}
                    outcomeClass={teamOutcomeClass(team2IsWinner, team1IsWinner)}
                />
            </div>
            {footer ? <div className="match-detail__hero-footer">{footer}</div> : null}
        </header>
    );
}

function HeroWatermark({
    side,
    shortName,
    logoUrl,
    iconLogoUrl,
}: {
    side: "left" | "right";
    shortName: string;
    logoUrl?: string | null;
    iconLogoUrl?: string | null;
}) {
    return (
        <div
            className={`match-detail__hero-watermark match-detail__hero-watermark--${side}`}
            aria-hidden="true"
        >
            <TeamLogo
                shortName={shortName}
                logoUrl={logoUrl}
                iconLogoUrl={iconLogoUrl}
                size={224}
                className="match-detail__hero-watermark-logo"
            />
        </div>
    );
}

function HeroTeamBlock({
    align,
    shortName,
    fullName,
    outcomeClass,
}: {
    align: "left" | "right";
    shortName: string;
    fullName: string;
    outcomeClass: string;
}) {
    const outcomeSuffix = outcomeClass ? ` ${outcomeClass}` : "";

    return (
        <div className={`match-detail__team match-detail__team--${align}${outcomeSuffix}`}>
            <div className="match-detail__team-identity">
                <span className="match-detail__team-short">{shortName}</span>
                <span className="match-detail__team-full">{fullName}</span>
            </div>
        </div>
    );
}
