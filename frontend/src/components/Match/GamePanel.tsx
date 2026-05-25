import type { ReactNode } from "react";
import { useState } from "react";
import { DamageBarsViewToggle, GameDamageBars, type TertiaryDamageView } from "@/components/Match/GameDamageBars.tsx";
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
    const [damageView, setDamageView] = useState<TertiaryDamageView>("game");

    if (gameLoading) {
        return (
            <GamePanelShell id={panelId} labelledBy={tabLabelId}>
                <div className="match-detail-loading" role="status" aria-live="polite">
                    <div className="match-detail-loading__spinner" aria-hidden="true" />
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
                <GameDraft
                    draft={gameDetails.draft}
                    team1Side={gameDetails.team1Side}
                    leftTeam={buildDraftTeamStats(gameSideTeams.left)}
                    rightTeam={buildDraftTeamStats(gameSideTeams.right)}
                />
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
                        <DamageBarsViewToggle tertiaryView={damageView} onViewChange={setDamageView} />
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
                        tertiaryView={damageView}
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

function buildDraftTeamStats(team: GameSideTeam) {
    return {
        shortName: team.shortName,
        kda: formatTeamKda(team.players),
        gold: formatGoldStat(sumPlayerStat(team.players, "goldEarned")),
    };
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

    return (
        <div className="match-detail__section-header">
            <GamePanelTeamHeader team={left} showWinBadge={leftWon} align="left" />
            <div className="match-detail__section-center">
                <span className="match-detail__section-game-label">
                    Game {gameNumber}
                    {gameDurationLabel ? <> - {gameDurationLabel}</> : null}
                </span>
            </div>
            <GamePanelTeamHeader team={right} showWinBadge={rightWon} align="right" />
        </div>
    );
}

function GamePanelTeamHeader({
    team,
    showWinBadge,
    align,
}: {
    team: GameSideTeam;
    showWinBadge: boolean;
    align: "left" | "right";
}) {
    const isRight = align === "right";
    const teamClass = `match-detail__section-team match-detail__section-team--with-logo${isRight ? " match-detail__section-team--right" : ""}`;
    const winBadge = showWinBadge ? <span className="match-detail__section-win-badge">WIN</span> : null;
    const logo = (
        <TeamLogo
            shortName={team.shortName}
            logoUrl={team.logoUrl}
            iconLogoUrl={team.iconLogoUrl}
            className="match-detail__section-team-logo"
        />
    );
    const name = <span className="match-detail__section-team-name">{team.shortName}</span>;

    if (isRight) {
        return (
            <span className={teamClass}>
                {winBadge}
                {name}
                {logo}
            </span>
        );
    }

    return (
        <span className={teamClass}>
            {logo}
            {name}
            {winBadge}
        </span>
    );
}
