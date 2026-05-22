import type { ReactNode } from "react";
import { GameDamageBars } from "@/components/Match/GameDamageBars.tsx";
import { GameDraft } from "@/components/Match/GameDraft.tsx";
import { GameObjectives } from "@/components/Match/GameObjectives.tsx";
import { GameScoreboard } from "@/components/Match/GameScoreboard.tsx";
import { isBlueSideFirst } from "@/components/Match/laneMatchupUtils.ts";
import {
    formatGameDurationLabel,
    formatGoldStat,
    formatTeamKda,
    sumPlayerStat,
} from "@/components/Match/matchDisplayUtils.ts";
import { TeamLogo } from "@/components/common/Logos.tsx";
import statGoldIcon from "@/assets/icons/lol-icons/lol-stat-gold.png";
import statKdaIcon from "@/assets/icons/lol-icons/lol-stat-kda.png";
import type { GameDetailsDto, GameListItem, MatchDetails, PlayerStatsDto } from "@/lib/api.ts";

interface GamePanelProps {
    match: MatchDetails;
    currentGame: GameListItem;
    gameDetails: GameDetailsDto | undefined;
    gameLoading: boolean;
    gameDetailsError: boolean;
}

interface GameSideTeam {
    teamNum: 1 | 2;
    shortName: string;
    logoUrl?: string | null;
    iconLogoUrl?: string | null;
    players: PlayerStatsDto[];
}

function resolveGameSideTeams(
    match: MatchDetails,
    gameDetails: GameDetailsDto,
): { left: GameSideTeam; right: GameSideTeam } {
    const blueFirst = isBlueSideFirst(gameDetails.team1Side);
    const team1: GameSideTeam = {
        teamNum: 1,
        shortName: match.team1ShortName,
        logoUrl: match.team1LogoUrl,
        iconLogoUrl: match.team1IconLogoUrl,
        players: gameDetails.team1Players,
    };
    const team2: GameSideTeam = {
        teamNum: 2,
        shortName: match.team2ShortName,
        logoUrl: match.team2LogoUrl,
        iconLogoUrl: match.team2IconLogoUrl,
        players: gameDetails.team2Players,
    };
    return {
        left: blueFirst ? team1 : team2,
        right: blueFirst ? team2 : team1,
    };
}

export function GamePanel({
    match,
    currentGame,
    gameDetails,
    gameLoading,
    gameDetailsError,
}: GamePanelProps) {
    const panelId = "match-detail-game-panel";
    const tabLabelId = `match-detail-tab-${currentGame.id}`;

    if (gameLoading) {
        return (
            <GamePanelShell id={panelId} labelledBy={tabLabelId}>
                <div className="match-detail-loading">
                    <div className="match-detail-loading__spinner" />
                    <span>Loading game data...</span>
                </div>
            </GamePanelShell>
        );
    }

    if (gameDetailsError) {
        return (
            <GamePanelShell id={panelId} labelledBy={tabLabelId}>
                <div className="match-detail__placeholder-body" role="alert">
                    <span>Could not load game details.</span>
                </div>
            </GamePanelShell>
        );
    }

    if (!gameDetails) {
        return <GamePanelShell id={panelId} labelledBy={tabLabelId} />;
    }

    const gameSideTeams = resolveGameSideTeams(match, gameDetails);
    const gameDurationLabel = formatGameDurationLabel(gameDetails);

    return (
        <GamePanelShell id={panelId} labelledBy={tabLabelId}>
            <section
                className="match-detail__section match-detail__section--stacked match-detail__section--draft-scoreboard"
                aria-labelledby="match-detail-draft-heading"
            >
                <h2 id="match-detail-draft-heading" className="sr-only">Draft and scoreboard</h2>
                <DraftScoreboardHeader
                    left={gameSideTeams.left}
                    right={gameSideTeams.right}
                    winningTeam={gameDetails.winningTeam}
                    gameNumber={currentGame.gameNumber}
                    gameDurationLabel={gameDurationLabel}
                />
                <GameDraft draft={gameDetails.draft} team1Side={gameDetails.team1Side} />
                <div className="match-detail__section-divider" aria-hidden="true" />
                <h2 id="match-detail-scoreboard-heading" className="sr-only">Scoreboard</h2>
                <div className="match-detail__subsection match-detail__subsection--scoreboard">
                    <GameScoreboard
                        team1Players={gameDetails.team1Players}
                        team2Players={gameDetails.team2Players}
                        team1Side={gameDetails.team1Side}
                        showDamage={false}
                    />
                </div>
            </section>

            <section
                className="match-detail__section match-detail__section--stacked match-detail__section--game-stats"
                aria-labelledby="match-detail-game-stats-heading"
            >
                <h2 id="match-detail-game-stats-heading" className="sr-only">Objectives and damage</h2>
                <div className="match-detail__three-col">
                    <div className="match-detail__stats-header match-detail__stats-header--objectives">
                        <h3 id="match-detail-objectives-title">Global objectives</h3>
                    </div>
                    <div className="match-detail__stats-header match-detail__stats-header--damage">
                        <h3>Damage breakdown</h3>
                    </div>
                    <section
                        className="match-detail__subsection match-detail__subsection--objectives-side"
                        aria-labelledby="match-detail-objectives-title"
                    >
                        <GameObjectives
                            match={match}
                            gameDetails={gameDetails}
                            loading={gameLoading}
                            error={gameDetailsError}
                        />
                    </section>
                    <GameDamageBars
                        team1Players={gameDetails.team1Players}
                        team2Players={gameDetails.team2Players}
                        team1Side={gameDetails.team1Side}
                    />
                </div>
                <div className="match-detail__section-divider" aria-hidden="true" />
                <section
                    className="match-detail__subsection match-detail__subsection--compact"
                    aria-labelledby="match-detail-highlights-title"
                >
                    <h3 id="match-detail-highlights-title" className="match-detail__section-title">
                        Highlights
                    </h3>
                    <div className="match-detail__placeholder-body match-detail__placeholder-body--compact">
                        <span>Coming soon</span>
                    </div>
                </section>
            </section>
        </GamePanelShell>
    );
}

function GamePanelShell({
    id,
    labelledBy,
    children,
}: {
    id: string;
    labelledBy: string;
    children?: ReactNode;
}) {
    return (
        <div
            id={id}
            role="tabpanel"
            aria-labelledby={labelledBy}
            className="match-detail__content"
        >
            {children}
        </div>
    );
}

function DraftScoreboardHeader({
    left,
    right,
    winningTeam,
    gameNumber,
    gameDurationLabel,
}: {
    left: GameSideTeam;
    right: GameSideTeam;
    winningTeam: number | null | undefined;
    gameNumber: number;
    gameDurationLabel: string | null;
}) {
    const leftWon = winningTeam === left.teamNum;
    const rightWon = winningTeam === right.teamNum;
    const leftKda = formatTeamKda(left.players);
    const rightKda = formatTeamKda(right.players);
    const leftGold = formatGoldStat(sumPlayerStat(left.players, "goldEarned"));
    const rightGold = formatGoldStat(sumPlayerStat(right.players, "goldEarned"));

    return (
        <div className="match-detail__section-header">
            <GamePanelTeamSummary
                team={left}
                kda={leftKda}
                gold={leftGold}
                showWinBadge={leftWon}
                align="left"
            />
            <div className="match-detail__section-center">
                <span className="match-detail__section-game-label">
                    Game {gameNumber}
                    {gameDurationLabel ? <> - {gameDurationLabel}</> : null}
                </span>
            </div>
            <GamePanelTeamSummary
                team={right}
                kda={rightKda}
                gold={rightGold}
                showWinBadge={rightWon}
                align="right"
            />
        </div>
    );
}

function GamePanelTeamSummary({
    team,
    kda,
    gold,
    showWinBadge,
    align,
}: {
    team: GameSideTeam;
    kda: string;
    gold: string;
    showWinBadge: boolean;
    align: "left" | "right";
}) {
    const isRight = align === "right";
    const teamClass = `match-detail__section-team match-detail__section-team--with-logo${isRight ? " match-detail__section-team--right" : ""}`;

    const winBadge = showWinBadge ? <span className="match-detail__section-win-badge">WIN</span> : null;
    const kdaBlock = (
        <span className="match-detail__section-kda" aria-label={`${team.shortName} kills, deaths and assists`}>
            <img src={statKdaIcon} alt="" aria-hidden="true" className="match-detail__section-stat-icon" />
            <span>{kda}</span>
        </span>
    );
    const goldBlock = (
        <span className="match-detail__section-gold" aria-label={`${team.shortName} total gold`}>
            <img src={statGoldIcon} alt="" aria-hidden="true" className="match-detail__section-stat-icon" />
            <span>{gold}</span>
        </span>
    );
    const logo = (
        <TeamLogo
            shortName={team.shortName}
            logoUrl={team.logoUrl}
            iconLogoUrl={team.iconLogoUrl}
            className="match-detail__section-team-logo"
        />
    );

    if (isRight) {
        return (
            <span className={teamClass}>
                {winBadge}
                {goldBlock}
                {kdaBlock}
                {logo}
            </span>
        );
    }

    return (
        <span className={teamClass}>
            {logo}
            {kdaBlock}
            {goldBlock}
            {winBadge}
        </span>
    );
}
