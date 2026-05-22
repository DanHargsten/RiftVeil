import { useEffect, useState } from "react";
import { useItemLookup } from "@/hooks/useItemLookup.ts";
import type { PlayerStatsDto } from "@/lib/api.ts";
import statCsIcon from "@/assets/icons/lol-icons/lol-stat-cs.png";
import statDamageIcon from "@/assets/icons/lol-icons/lol-stat-damage.png";
import statGoldIcon from "@/assets/icons/lol-icons/lol-stat-gold.png";
import statItemsIcon from "@/assets/icons/lol-icons/lol-stat-items.png";
import statKdaIcon from "@/assets/icons/lol-icons/lol-stat-kda.png";
import statPlayerIcon from "@/assets/icons/lol-icons/lol-stat-player.png";
import { formatGoldStat } from "@/components/Match/matchDisplayUtils.ts";
import {
    buildItemSlots,
    formatKdaRatio,
    scoreboardClass,
} from "@/components/Match/scoreboardUtils.ts";
import {
    ROLE_META,
    buildLaneMatchupRows,
    formatDamage,
    formatPlayerName,
    type LaneRole,
} from "@/components/Match/laneMatchupUtils.ts";

interface GameScoreboardProps {
    team1Players: PlayerStatsDto[];
    team2Players: PlayerStatsDto[];
    team1Side: string | null;
    showDamage?: boolean;
}

export function GameScoreboard({
    team1Players,
    team2Players,
    team1Side,
    showDamage = true,
}: GameScoreboardProps) {
    const { getItemIconUrl, ddragonVersion, hasResolvedVersion } = useItemLookup();
    const roleRows = buildLaneMatchupRows(team1Players, team2Players, team1Side);

    if (!hasResolvedVersion) {
        return (
            <section className="scoreboard" role="region" aria-label="Player lane matchups">
                <header className="scoreboard__matchup-header">
                    <div className="scoreboard__matchup-center-label">Lane matchups</div>
                </header>
                <div className="scoreboard__loading-state">Loading assets...</div>
            </section>
        );
    }

    return (
        <section className="scoreboard" role="region" aria-label="Player lane matchups">
            <header className="scoreboard__matchup-header">
                <div className="scoreboard__matchup-center-label">Lane matchups</div>
            </header>

            <div className="scoreboard__table-wrap">
                <table className="scoreboard__table scoreboard__table--matchups">
                    <caption className="sr-only">
                        Lane-by-lane player comparison with KDA, creep score, gold and items.
                    </caption>
                    <ScoreboardColgroup showDamage={showDamage} />
                    <ScoreboardHead showDamage={showDamage} />
                    <tbody>
                        {roleRows.map(({ role, leftPlayer, rightPlayer }) => (
                            <LaneMatchupRow
                                key={role}
                                role={role}
                                leftPlayer={leftPlayer}
                                rightPlayer={rightPlayer}
                                showDamage={showDamage}
                                getItemIconUrl={getItemIconUrl}
                                ddragonVersion={ddragonVersion}
                            />
                        ))}
                    </tbody>
                </table>
            </div>
        </section>
    );
}

function ScoreboardColgroup({ showDamage }: { showDamage: boolean }) {
    return (
        <colgroup>
            <col className="scoreboard__col--player" />
            <col className="scoreboard__col--kda" />
            <col className="scoreboard__col--cs" />
            <col className="scoreboard__col--gold" />
            <col className="scoreboard__col--items" />
            {showDamage ? <col className="scoreboard__col--damage" /> : null}
            <col className="scoreboard__col--role" />
            {showDamage ? <col className="scoreboard__col--damage" /> : null}
            <col className="scoreboard__col--items" />
            <col className="scoreboard__col--gold" />
            <col className="scoreboard__col--cs" />
            <col className="scoreboard__col--kda" />
            <col className="scoreboard__col--player" />
        </colgroup>
    );
}

function ScoreboardHead({ showDamage }: { showDamage: boolean }) {
    return (
        <thead>
            <tr>
                <ScoreboardHeaderCell label="Player" icon={statPlayerIcon} />
                <ScoreboardHeaderCell label="K/D/A" icon={statKdaIcon} />
                <ScoreboardHeaderCell label="CS" icon={statCsIcon} />
                <ScoreboardHeaderCell label="Gold" icon={statGoldIcon} />
                <ScoreboardHeaderCell label="Items" icon={statItemsIcon} />
                {showDamage ? <ScoreboardHeaderCell label="Damage" icon={statDamageIcon} /> : null}
                <th scope="col" className="scoreboard__th scoreboard__th--role">
                    <span className="sr-only">Role</span>
                </th>
                {showDamage ? <ScoreboardHeaderCell label="Damage" icon={statDamageIcon} /> : null}
                <ScoreboardHeaderCell label="Items" icon={statItemsIcon} align="right" />
                <ScoreboardHeaderCell label="Gold" icon={statGoldIcon} align="right" />
                <ScoreboardHeaderCell label="CS" icon={statCsIcon} align="right" />
                <ScoreboardHeaderCell label="K/D/A" icon={statKdaIcon} align="right" />
                <ScoreboardHeaderCell label="Player" icon={statPlayerIcon} align="right" />
            </tr>
        </thead>
    );
}

function ScoreboardHeaderCell({
    label,
    icon,
    align = "left",
}: {
    label: string;
    icon: string;
    align?: "left" | "right";
}) {
    return (
        <th scope="col" className={`scoreboard__th scoreboard__th--${align}`}>
            <span className={`scoreboard__th-content scoreboard__th-content--${align}`}>
                <img
                    src={icon}
                    alt=""
                    className="scoreboard__th-icon"
                    width={14}
                    height={14}
                    aria-hidden="true"
                />
                <span>{label}</span>
            </span>
        </th>
    );
}

function LaneMatchupRow({
    role,
    leftPlayer,
    rightPlayer,
    showDamage,
    getItemIconUrl,
    ddragonVersion,
}: {
    role: LaneRole;
    leftPlayer: PlayerStatsDto | null;
    rightPlayer: PlayerStatsDto | null;
    showDamage: boolean;
    getItemIconUrl: (name: string) => string | null;
    ddragonVersion: string;
}) {
    return (
        <tr className="scoreboard__row scoreboard__row--lane">
            <PlayerCell player={leftPlayer} side="left" ddragonVersion={ddragonVersion} />
            <KdaCell player={leftPlayer} side="left" />
            <NumericCell value={leftPlayer?.creepScore} />
            <NumericCell value={leftPlayer ? formatGoldStat(leftPlayer.goldEarned) : null} isGold />
            <ItemsCell player={leftPlayer} getItemIconUrl={getItemIconUrl} side="left" />
            {showDamage ? <DamageCell player={leftPlayer} side="left" /> : null}
            <td className="scoreboard__role-cell">
                <img
                    src={ROLE_META[role].icon}
                    alt={ROLE_META[role].label}
                    className="scoreboard__role-icon"
                    width={22}
                    height={22}
                />
            </td>
            {showDamage ? <DamageCell player={rightPlayer} side="right" /> : null}
            <ItemsCell player={rightPlayer} getItemIconUrl={getItemIconUrl} side="right" />
            <NumericCell value={rightPlayer ? formatGoldStat(rightPlayer.goldEarned) : null} isGold align="right" />
            <NumericCell value={rightPlayer?.creepScore} align="right" />
            <KdaCell player={rightPlayer} side="right" />
            <PlayerCell player={rightPlayer} side="right" ddragonVersion={ddragonVersion} />
        </tr>
    );
}

function PlayerCell({
    player,
    side,
    ddragonVersion,
}: {
    player: PlayerStatsDto | null;
    side: "left" | "right";
    ddragonVersion: string;
}) {
    const cellClass = scoreboardClass("scoreboard__player-cell", side);

    if (!player) {
        return <td className={cellClass}>—</td>;
    }

    return (
        <td className={cellClass}>
            <div className={scoreboardClass("scoreboard__player-inner", side)}>
                <ChampionIcon champion={player.champion} size={40} ddragonVersion={ddragonVersion} />
                <div className="scoreboard__player-info">
                    <span className="scoreboard__player-name">{formatPlayerName(player.playerName)}</span>
                    <span className="scoreboard__player-champ">{player.champion}</span>
                </div>
            </div>
        </td>
    );
}

function KdaCell({ player, side }: { player: PlayerStatsDto | null; side: "left" | "right" }) {
    const cellClass = scoreboardClass("scoreboard__kda-cell", side);

    if (!player) {
        return <td className={cellClass}>—</td>;
    }

    const kdaRatio = formatKdaRatio(player);
    const ariaLabel = `${player.kills} kills, ${player.deaths} deaths, ${player.assists} assists, ratio ${kdaRatio}`;

    return (
        <td className={cellClass} aria-label={ariaLabel}>
            <div className={scoreboardClass("scoreboard__kda-inner", side)}>
                <span className="scoreboard__kda">
                    <span className="scoreboard__kda-kills">{player.kills}</span>
                    <span className="scoreboard__kda-deaths">{player.deaths}</span>
                    <span className="scoreboard__kda-assists">{player.assists}</span>
                </span>
                <span className="scoreboard__kda-ratio">{kdaRatio}</span>
            </div>
        </td>
    );
}

function NumericCell({
    value,
    align = "left",
    isGold = false,
}: {
    value: number | string | null | undefined;
    align?: "left" | "right";
    isGold?: boolean;
}) {
    const goldModifier = isGold ? "scoreboard__num-cell--gold" : "";
    const className = scoreboardClass("scoreboard__num-cell", undefined, `scoreboard__num-cell--${align} ${goldModifier}`.trim());

    return <td className={className}>{value ?? "—"}</td>;
}

function ItemsCell({
    player,
    side,
    getItemIconUrl,
}: {
    player: PlayerStatsDto | null;
    side: "left" | "right";
    getItemIconUrl: (name: string) => string | null;
}) {
    const cellClass = scoreboardClass("scoreboard__items-cell", side);

    if (!player) {
        return <td className={cellClass}>—</td>;
    }

    const slots = buildItemSlots(player, side);

    return (
        <td className={cellClass}>
            <div className={scoreboardClass("scoreboard__items-inner", side)}>
                {slots.map((slot, slotIndex) => (
                    <ItemIcon
                        key={`${player.playerName}-${side}-item-${slotIndex}`}
                        name={slot.name}
                        iconUrl={getItemIconUrl(slot.name)}
                        isTrinket={slot.isTrinket}
                    />
                ))}
            </div>
        </td>
    );
}

function DamageCell({ player, side }: { player: PlayerStatsDto | null; side: "left" | "right" }) {
    return (
        <td className={scoreboardClass("scoreboard__damage-cell", side)}>
            {player ? formatDamage(player.damageDealtToChampions) : "—"}
        </td>
    );
}

function ItemIcon({
    name,
    iconUrl,
    isTrinket = false,
}: {
    name: string;
    iconUrl: string | null;
    isTrinket?: boolean;
}) {
    const [hasError, setHasError] = useState(false);
    const trinketClass = isTrinket ? "scoreboard__item-icon--trinket" : "";

    useEffect(() => {
        setHasError(false);
    }, [iconUrl, name]);

    if (!iconUrl || hasError) {
        return (
            <div
                className={`scoreboard__item-icon scoreboard__item-icon--missing ${trinketClass}`.trim()}
                role="img"
                aria-label={name}
            />
        );
    }

    return (
        <div className={`scoreboard__item-icon ${trinketClass}`.trim()}>
            <img
                src={iconUrl}
                alt={name}
                width={24}
                height={24}
                onError={() => setHasError(true)}
            />
        </div>
    );
}

function ChampionIcon({
    champion,
    size,
    ddragonVersion,
}: {
    champion: string;
    size: number;
    ddragonVersion: string;
}) {
    const [hasError, setHasError] = useState(false);
    const url = championIconUrl(champion, ddragonVersion);

    useEffect(() => {
        setHasError(false);
    }, [url]);

    if (hasError) {
        return (
            <div
                className="scoreboard__champ-icon scoreboard__champ-icon--missing"
                role="img"
                aria-label={champion}
            />
        );
    }

    return (
        <img
            src={url}
            alt=""
            width={size}
            height={size}
            className="scoreboard__champ-icon"
            onError={() => setHasError(true)}
        />
    );
}

function championIconUrl(champion: string, ddragonVersion: string): string {
    const normalized = champion
        .replace(/[^a-zA-Z0-9]/g, "")
        .replace(/^(.)/, (c) => c.toUpperCase());

    const overrides: Record<string, string> = {
        Wukong: "MonkeyKing",
        Nunu: "Nunu",
        Renata: "Renata",
    };

    const ddName = overrides[champion] ?? normalized;
    return `https://ddragon.leagueoflegends.com/cdn/${ddragonVersion}/img/champion/${ddName}.png`;
}
