import { useEffect, useState } from "react";
import { useItemLookup } from "@/hooks/useItemLookup.ts";
import type { PlayerStatsDto } from "@/lib/api.ts";
import { resolveTeamLogoSrc, teamLogoFallbackSrc } from "@/lib/teamLogo.ts";
import statCsIcon from "@/assets/icons/lol-icons/lol-stat-cs.png";
import statDamageIcon from "@/assets/icons/lol-icons/lol-stat-damage.png";
import statGoldIcon from "@/assets/icons/lol-icons/lol-stat-gold.png";
import statItemsIcon from "@/assets/icons/lol-icons/lol-stat-items.png";
import statKdaIcon from "@/assets/icons/lol-icons/lol-stat-kda.png";
import statPlayerIcon from "@/assets/icons/lol-icons/lol-stat-player.png";
import roleBotIcon from "@/assets/icons/lol-icons/role-bot.png";
import roleJungleIcon from "@/assets/icons/lol-icons/role-jungle.png";
import roleMidIcon from "@/assets/icons/lol-icons/role-mid.png";
import roleSupportIcon from "@/assets/icons/lol-icons/role-support.png";
import roleTopIcon from "@/assets/icons/lol-icons/role-top.png";

interface GameScoreboardProps {
    team1Players: PlayerStatsDto[];
    team2Players: PlayerStatsDto[];
    team1Side: string | null;
    team1ShortName: string;
    team2ShortName: string;
    team1LogoUrl?: string | null;
    team2LogoUrl?: string | null;
    team1IconLogoUrl?: string | null;
    team2IconLogoUrl?: string | null;
    showDamage?: boolean;
}

export function GameScoreboard({
    team1Players,
    team2Players,
    team1Side,
    team1ShortName,
    team2ShortName,
    team1LogoUrl,
    team2LogoUrl,
    team1IconLogoUrl,
    team2IconLogoUrl,
    showDamage = true,
}: GameScoreboardProps) {
    const { getItemIconUrl, ddragonVersion, hasResolvedVersion } = useItemLookup();

    const blueFirst = team1Side == null || team1Side.toLowerCase() === "blue";
    const leftTeamShortName = blueFirst ? team1ShortName : team2ShortName;
    const rightTeamShortName = blueFirst ? team2ShortName : team1ShortName;
    const leftTeamLogoUrl = blueFirst ? team1LogoUrl : team2LogoUrl;
    const rightTeamLogoUrl = blueFirst ? team2LogoUrl : team1LogoUrl;
    const leftTeamIconLogoUrl = blueFirst ? team1IconLogoUrl : team2IconLogoUrl;
    const rightTeamIconLogoUrl = blueFirst ? team2IconLogoUrl : team1IconLogoUrl;
    const leftPlayersByRole = allocatePlayersByRole(blueFirst ? team1Players : team2Players);
    const rightPlayersByRole = allocatePlayersByRole(blueFirst ? team2Players : team1Players);
    const roleRows = ROLE_ORDER.map((role) => ({
        role,
        leftPlayer: leftPlayersByRole[role],
        rightPlayer: rightPlayersByRole[role],
    }));

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
                <div className="scoreboard__watermark-zone scoreboard__watermark-zone--left" aria-hidden="true">
                    <TeamWatermark
                        shortName={leftTeamShortName}
                        logoUrl={leftTeamLogoUrl}
                        iconLogoUrl={leftTeamIconLogoUrl}
                    />
                </div>
                <div className="scoreboard__watermark-zone scoreboard__watermark-zone--right" aria-hidden="true">
                    <TeamWatermark
                        shortName={rightTeamShortName}
                        logoUrl={rightTeamLogoUrl}
                        iconLogoUrl={rightTeamIconLogoUrl}
                    />
                </div>
                <table className="scoreboard__table scoreboard__table--matchups">
                <caption className="sr-only">
                    Lane-by-lane player comparison with KDA, creep score, gold and items.
                </caption>
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

type LaneRole = "top" | "jungle" | "mid" | "bot" | "support";

const ROLE_ORDER: LaneRole[] = ["top", "jungle", "mid", "bot", "support"];

const ROLE_META: Record<LaneRole, { label: string; icon: string }> = {
    top: { label: "Top", icon: roleTopIcon },
    jungle: { label: "Jungle", icon: roleJungleIcon },
    mid: { label: "Mid", icon: roleMidIcon },
    bot: { label: "Bot", icon: roleBotIcon },
    support: { label: "Support", icon: roleSupportIcon },
};

function TeamWatermark({
    shortName,
    logoUrl,
    iconLogoUrl,
}: {
    shortName: string;
    logoUrl?: string | null;
    iconLogoUrl?: string | null;
}) {
    const [src, setSrc] = useState(() =>
        resolveTeamLogoSrc(logoUrl, iconLogoUrl, shortName, "icon"));

    useEffect(() => {
        setSrc(resolveTeamLogoSrc(logoUrl, iconLogoUrl, shortName, "icon"));
    }, [logoUrl, iconLogoUrl, shortName]);

    return (
        <img
            src={src}
            alt=""
            aria-hidden="true"
            className="scoreboard__team-watermark"
            onError={() => {
                setSrc((current) => {
                    const next = teamLogoFallbackSrc(
                        current,
                        shortName,
                        logoUrl,
                        iconLogoUrl,
                        "icon",
                    );
                    return next === current ? current : next;
                });
            }}
        />
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
            <NumericCell value={leftPlayer ? formatGold(leftPlayer.goldEarned) : null} isGold />
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
            <NumericCell value={rightPlayer ? formatGold(rightPlayer.goldEarned) : null} isGold align="right" />
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
    if (!player) {
        return <td className={`scoreboard__player-cell scoreboard__player-cell--${side}`}>—</td>;
    }

    return (
        <td className={`scoreboard__player-cell scoreboard__player-cell--${side}`}>
            <div className={`scoreboard__player-inner scoreboard__player-inner--${side}`}>
                <ChampionIcon
                    champion={player.champion}
                    size={40}
                    ddragonVersion={ddragonVersion}
                />
                <div className="scoreboard__player-info">
                    <span className="scoreboard__player-name">{player.playerName.replace(/\s*\(.*?\)/, "").trim()}</span>
                    <span className="scoreboard__player-champ">{player.champion}</span>
                </div>
            </div>
        </td>
    );
}

function KdaCell({ player, side }: { player: PlayerStatsDto | null; side: "left" | "right" }) {
    if (!player) {
        return <td className={`scoreboard__kda-cell scoreboard__kda-cell--${side}`}>—</td>;
    }

    const kdaRatio = player.deaths === 0
        ? "Perfect"
        : ((player.kills + player.assists) / player.deaths).toFixed(1);

    return (
        <td
            className={`scoreboard__kda-cell scoreboard__kda-cell--${side}`}
            aria-label={`${player.kills} kills, ${player.deaths} deaths, ${player.assists} assists, ratio ${kdaRatio}`}
        >
            <div className={`scoreboard__kda-inner scoreboard__kda-inner--${side}`}>
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
    const className = [
        "scoreboard__num-cell",
        `scoreboard__num-cell--${align}`,
        isGold ? "scoreboard__num-cell--gold" : "",
    ].join(" ").trim();

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
    if (!player) {
        return <td className={`scoreboard__items-cell scoreboard__items-cell--${side}`}>—</td>;
    }

    const items = player.itemIds
        ? player.itemIds.split(";").filter(Boolean)
        : [];
    const trinket = player.trinketId ?? null;
    const slots = side === "right"
        ? [
            ...(trinket ? [{ name: trinket, isTrinket: true }] : []),
            ...items.slice().reverse().map((name) => ({ name, isTrinket: false })),
        ]
        : [
            ...items.map((name) => ({ name, isTrinket: false })),
            ...(trinket ? [{ name: trinket, isTrinket: true }] : []),
        ];

    return (
        <td className={`scoreboard__items-cell scoreboard__items-cell--${side}`}>
            <div className={`scoreboard__items-inner scoreboard__items-inner--${side}`}>
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
        <td className={`scoreboard__damage-cell scoreboard__damage-cell--${side}`}>
            {player ? formatDamage(player.damageDealtToChampions) : "—"}
        </td>
    );
}

function allocatePlayersByRole(players: PlayerStatsDto[]): Record<LaneRole, PlayerStatsDto | null> {
    const byRole: Record<LaneRole, PlayerStatsDto | null> = {
        top: null,
        jungle: null,
        mid: null,
        bot: null,
        support: null,
    };
    const leftovers: PlayerStatsDto[] = [];

    for (const player of players) {
        const role = normalizeRole(player.ingameRole);
        if (!role || byRole[role] != null) {
            leftovers.push(player);
            continue;
        }
        byRole[role] = player;
    }

    for (const role of ROLE_ORDER) {
        if (byRole[role] == null && leftovers.length > 0) {
            byRole[role] = leftovers.shift() ?? null;
        }
    }

    return byRole;
}

function normalizeRole(rawRole: string | null | undefined): LaneRole | null {
    if (!rawRole) return null;
    const role = rawRole.toLowerCase().trim();
    if (["top", "toplane", "top lane"].includes(role)) return "top";
    if (["jungle", "jungler", "jgl"].includes(role)) return "jungle";
    if (["mid", "middle", "midlane", "mid lane"].includes(role)) return "mid";
    if (["bot", "bottom", "adc", "carry", "bottom lane", "bot lane"].includes(role)) return "bot";
    if (["support", "sup", "supp"].includes(role)) return "support";
    return null;
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

    useEffect(() => {
        setHasError(false);
    }, [iconUrl, name]);

    if (!iconUrl || hasError) {
        return (
            <div
                className={`scoreboard__item-icon scoreboard__item-icon--missing ${isTrinket ? "scoreboard__item-icon--trinket" : ""}`}
                role="img"
                aria-label={name}
            />
        );
    }

    return (
        <div
            className={`scoreboard__item-icon ${isTrinket ? "scoreboard__item-icon--trinket" : ""}`}
        >
            <img
                src={iconUrl}
                alt={name}
                width={24}
                height={24}
                onError={() => {
                    setHasError(true);
                }}
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

    const normalized = champion
        .replace(/[^a-zA-Z0-9]/g, "")
        .replace(/^(.)/, (c) => c.toUpperCase());

    const overrides: Record<string, string> = {
        "Wukong": "MonkeyKing",
        "Nunu": "Nunu",
        "Renata": "Renata",
    };

    const ddName = overrides[champion] ?? normalized;
    const url = `https://ddragon.leagueoflegends.com/cdn/${ddragonVersion}/img/champion/${ddName}.png`;

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
            onError={() => {
                setHasError(true);
            }}
        />
    );
}

function formatGold(gold: number): string {
    if (gold >= 1000) return `${(gold / 1000).toFixed(1)}k`;
    return String(gold);
}

function formatDamage(dmg: number): string {
    if (dmg >= 1000) return `${(dmg / 1000).toFixed(1)}k`;
    return String(dmg);
}
