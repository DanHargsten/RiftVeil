import { useMemo, type ReactNode } from "react";

import type { PlayerStatsDto } from "@/lib/api.ts";
import {
    barWidthPct,
    barWidthStyle,
    computeDamageScales,
    getScaleForMode,
    laneSharePct,
    type DamageScales,
    type TertiaryDamageView,
} from "@/components/Match/damageBarUtils.ts";
import {
    ROLE_META,
    buildLaneMatchupRows,
    formatDamage,
    formatPlayerName,
    type LaneMatchupRow,
} from "@/components/Match/laneMatchupUtils.ts";
import { modeButtonClass } from "@/components/Match/matchDisplayUtils.ts";

const TERTIARY_VIEWS: { id: TertiaryDamageView; label: string }[] = [
    { id: "game", label: "Game" },
    { id: "team", label: "Team" },
];

export type { TertiaryDamageView };

export function DamageBarsViewToggle({
    tertiaryView,
    onViewChange,
}: {
    tertiaryView: TertiaryDamageView;
    onViewChange: (view: TertiaryDamageView) => void;
}) {
    return (
        <div role="tablist" aria-label="Game or team damage" className="damage-bars__modes">
            {TERTIARY_VIEWS.map(({ id, label }) => (
                <button
                    key={id}
                    type="button"
                    role="tab"
                    aria-selected={tertiaryView === id}
                    className={modeButtonClass(tertiaryView === id)}
                    onClick={() => onViewChange(id)}
                >
                    {label}
                </button>
            ))}
        </div>
    );
}

interface GameDamageBarsProps {
    team1Players: PlayerStatsDto[];
    team2Players: PlayerStatsDto[];
    team1Side: string | null;
    tertiaryView: TertiaryDamageView;
}

export function GameDamageBars({
    team1Players,
    team2Players,
    team1Side,
    tertiaryView,
}: GameDamageBarsProps) {
    const roleRows = useMemo(
        () => buildLaneMatchupRows(team1Players, team2Players, team1Side),
        [team1Players, team2Players, team1Side],
    );
    const scales = useMemo(
        () => computeDamageScales(team1Players, team2Players, team1Side),
        [team1Players, team2Players, team1Side],
    );

    return (
        <div className="damage-bars damage-bars--grid-cells">
            <div className="damage-bars__matchup" role="group" aria-label="Damage breakdown charts">
                <div className="damage-bars__matchup-rows">
                    {roleRows.map((row) => (
                        <DamageMatchupRow
                            key={row.role}
                            row={row}
                            tertiaryView={tertiaryView}
                            scales={scales}
                        />
                    ))}
                </div>
            </div>
        </div>
    );
}

function DamageMatchupRow({
    row,
    tertiaryView,
    scales,
}: {
    row: LaneMatchupRow;
    tertiaryView: TertiaryDamageView;
    scales: DamageScales;
}) {
    const { role, leftPlayer, rightPlayer } = row;

    return (
        <div className="damage-bars__matchup-row">
            <LaneMatchupRow leftPlayer={leftPlayer} rightPlayer={rightPlayer} />
            <RoleRailIcon role={role} />
            <ScaleMatchupRow
                mode={tertiaryView}
                scales={scales}
                leftPlayer={leftPlayer}
                rightPlayer={rightPlayer}
            />
        </div>
    );
}

function RoleRailIcon({ role }: { role: LaneMatchupRow["role"] }) {
    return (
        <div className="damage-bars__matchup-rail-cell" aria-hidden="true">
            <img
                src={ROLE_META[role].icon}
                alt=""
                className="damage-bars__role-icon damage-bars__role-icon--rail"
                width={22}
                height={22}
            />
        </div>
    );
}

function LaneBar({ leftPct, rightPct }: { leftPct: number; rightPct: number }) {
    return (
        <div className="damage-bars__bar damage-bars__bar--lane">
            <div
                className="damage-bars__segment damage-bars__segment--left"
                style={barWidthStyle(leftPct)}
            />
            <div
                className="damage-bars__segment damage-bars__segment--right"
                style={barWidthStyle(rightPct)}
            />
        </div>
    );
}

function SplitBar({ leftPct, rightPct }: { leftPct: number; rightPct: number }) {
    return (
        <div className="damage-bars__tracks-wrap">
            <div className="damage-bars__tracks">
                <div className="damage-bars__track damage-bars__track--left">
                    <div
                        className="damage-bars__fill damage-bars__fill--left"
                        style={barWidthStyle(leftPct)}
                    />
                </div>
                <div className="damage-bars__track damage-bars__track--right">
                    <div
                        className="damage-bars__fill damage-bars__fill--right"
                        style={barWidthStyle(rightPct)}
                    />
                </div>
            </div>
        </div>
    );
}

function MatchupRowFrame({
    valuesClassName,
    leftPlayer,
    rightPlayer,
    bar,
}: {
    valuesClassName: string;
    leftPlayer: PlayerStatsDto | null;
    rightPlayer: PlayerStatsDto | null;
    bar: ReactNode;
}) {
    const leftDmg = leftPlayer?.damageDealtToChampions ?? 0;
    const rightDmg = rightPlayer?.damageDealtToChampions ?? 0;
    const leftName = leftPlayer ? formatPlayerName(leftPlayer.playerName) : "—";
    const rightName = rightPlayer ? formatPlayerName(rightPlayer.playerName) : "—";

    return (
        <div className="damage-bars__row">
            <div className={valuesClassName}>
                <span className="damage-bars__value damage-bars__value--left">
                    {leftPlayer ? formatDamage(leftDmg) : "—"}
                </span>
                <span className="damage-bars__value damage-bars__value--right">
                    {rightPlayer ? formatDamage(rightDmg) : "—"}
                </span>
            </div>
            <span className="damage-bars__name damage-bars__name--left">{leftName}</span>
            <div className="damage-bars__bar-area" aria-hidden="true">
                {bar}
            </div>
            <span className="damage-bars__name damage-bars__name--right">{rightName}</span>
        </div>
    );
}

function LaneMatchupRow({
    leftPlayer,
    rightPlayer,
}: {
    leftPlayer: PlayerStatsDto | null;
    rightPlayer: PlayerStatsDto | null;
}) {
    const leftDmg = leftPlayer?.damageDealtToChampions ?? 0;
    const rightDmg = rightPlayer?.damageDealtToChampions ?? 0;
    const { leftPct, rightPct } = laneSharePct(leftDmg, rightDmg);

    return (
        <MatchupRowFrame
            valuesClassName="damage-bars__values-over-bar damage-bars__values-over-bar--lane"
            leftPlayer={leftPlayer}
            rightPlayer={rightPlayer}
            bar={<LaneBar leftPct={leftPct} rightPct={rightPct} />}
        />
    );
}

function ScaleMatchupRow({
    mode,
    scales,
    leftPlayer,
    rightPlayer,
}: {
    mode: TertiaryDamageView;
    scales: DamageScales;
    leftPlayer: PlayerStatsDto | null;
    rightPlayer: PlayerStatsDto | null;
}) {
    const leftDmg = leftPlayer?.damageDealtToChampions ?? 0;
    const rightDmg = rightPlayer?.damageDealtToChampions ?? 0;
    const leftScale = getScaleForMode(mode, scales, "left");
    const rightScale = getScaleForMode(mode, scales, "right");
    const leftPct = barWidthPct(leftDmg, leftScale);
    const rightPct = barWidthPct(rightDmg, rightScale);

    return (
        <MatchupRowFrame
            valuesClassName="damage-bars__values-over-bar"
            leftPlayer={leftPlayer}
            rightPlayer={rightPlayer}
            bar={<SplitBar leftPct={leftPct} rightPct={rightPct} />}
        />
    );
}
