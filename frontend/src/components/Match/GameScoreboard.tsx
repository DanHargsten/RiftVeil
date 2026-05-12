import { TeamLogo } from "@/components/common/Logos.tsx";
import { useItemLookup } from "@/hooks/useItemLookup.ts";
import type { PlayerStatsDto, TeamStatsDto } from "@/lib/api.ts";

interface GameScoreboardProps {
    team1Name: string;
    team2Name: string;
    team1Players: PlayerStatsDto[];
    team2Players: PlayerStatsDto[];
    team1Stats: TeamStatsDto | null;
    team2Stats: TeamStatsDto | null;
    winningTeam: number | null;
    team1Side: string | null;
}

export function GameScoreboard({
    team1Name,
    team2Name,
    team1Players,
    team2Players,
    team1Stats,
    team2Stats,
    winningTeam,
    team1Side,
}: GameScoreboardProps) {
    const { getItemIconUrl } = useItemLookup();
    const team1Gold = team1Stats?.totalGoldEarned
        ?? team1Players.reduce((sum, player) => sum + player.goldEarned, 0);
    const team2Gold = team2Stats?.totalGoldEarned
        ?? team2Players.reduce((sum, player) => sum + player.goldEarned, 0);
    const team1Kills = team1Stats?.totalKills
        ?? team1Players.reduce((sum, player) => sum + player.kills, 0);
    const team2Kills = team2Stats?.totalKills
        ?? team2Players.reduce((sum, player) => sum + player.kills, 0);
    const team1Deaths = team1Stats?.totalDeaths
        ?? team1Players.reduce((sum, player) => sum + player.deaths, 0);
    const team2Deaths = team2Stats?.totalDeaths
        ?? team2Players.reduce((sum, player) => sum + player.deaths, 0);
    const team1Assists = team1Stats?.totalAssists
        ?? team1Players.reduce((sum, player) => sum + player.assists, 0);
    const team2Assists = team2Stats?.totalAssists
        ?? team2Players.reduce((sum, player) => sum + player.assists, 0);

    const blueFirst = team1Side === "Blue";

    return (
        <div className="scoreboard" role="region" aria-label="Player statistics by team">
            {blueFirst ? (
                <>
                    <ScoreboardTeam
                        teamName={team1Name}
                        teamShortName={team1Name}
                        players={team1Players}
                        isWinner={winningTeam === 1}
                        totalKills={team1Kills}
                        totalDeaths={team1Deaths}
                        totalAssists={team1Assists}
                        totalGold={team1Gold}
                        side="left"
                        getItemIconUrl={getItemIconUrl}
                    />
                    <ScoreboardTeam
                        teamName={team2Name}
                        teamShortName={team2Name}
                        players={team2Players}
                        isWinner={winningTeam === 2}
                        totalKills={team2Kills}
                        totalDeaths={team2Deaths}
                        totalAssists={team2Assists}
                        totalGold={team2Gold}
                        side="right"
                        getItemIconUrl={getItemIconUrl}
                    />
                </>
            ) : (
                <>
                    <ScoreboardTeam
                        teamName={team2Name}
                        teamShortName={team2Name}
                        players={team2Players}
                        isWinner={winningTeam === 2}
                        totalKills={team2Kills}
                        totalDeaths={team2Deaths}
                        totalAssists={team2Assists}
                        totalGold={team2Gold}
                        side="left"
                        getItemIconUrl={getItemIconUrl}
                    />
                    <ScoreboardTeam
                        teamName={team1Name}
                        teamShortName={team1Name}
                        players={team1Players}
                        isWinner={winningTeam === 1}
                        totalKills={team1Kills}
                        totalDeaths={team1Deaths}
                        totalAssists={team1Assists}
                        totalGold={team1Gold}
                        side="right"
                        getItemIconUrl={getItemIconUrl}
                    />
                </>
            )}
        </div>
    );
}

interface ScoreboardTeamProps {
    teamName: string;
    teamShortName: string;
    players: PlayerStatsDto[];
    isWinner: boolean;
    totalKills: number;
    totalDeaths: number;
    totalAssists: number;
    totalGold: number;
    side: "left" | "right";
    getItemIconUrl: (name: string) => string | null;
}

function ScoreboardTeam({
    teamName,
    teamShortName,
    players,
    isWinner,
    side,
    getItemIconUrl,
}: ScoreboardTeamProps) {
    const maxDamage = Math.max(
        ...players.map((player) => player.damageDealtToChampions),
        1,
    );
    const headingId = `scoreboard-team-${side}`;

    return (
        <section
            className={`scoreboard__team scoreboard__team--${side}`}
            aria-labelledby={headingId}
        >
            <header className="scoreboard__team-header">
                <TeamLogo shortName={teamShortName} className="scoreboard__team-logo" />
                <h3 id={headingId} className="scoreboard__team-name">
                    {teamName}
                </h3>

                {isWinner && (
                    <span className="scoreboard__winner-badge">
                        <span aria-hidden="true">WIN</span>
                        <span className="sr-only"> — match winner</span>
                    </span>
                )}
            </header>

            <table className="scoreboard__table">
                <caption className="sr-only">
                    {teamName}: player, K/D/A, creep score, gold, items, damage to champions
                </caption>
                <colgroup>
                    <col className="scoreboard__col scoreboard__col--player" />
                    <col className="scoreboard__col scoreboard__col--kda" />
                    <col className="scoreboard__col scoreboard__col--cs" />
                    <col className="scoreboard__col scoreboard__col--gold" />
                    <col className="scoreboard__col scoreboard__col--items" />
                    <col className="scoreboard__col scoreboard__col--damage" />
                </colgroup>
                <thead>
                    <tr>
                        <th scope="col" className="scoreboard__th">Player</th>
                        <th scope="col" className="scoreboard__th">K/D/A</th>
                        <th scope="col" className="scoreboard__th">CS</th>
                        <th scope="col" className="scoreboard__th">Gold</th>
                        <th scope="col" className="scoreboard__th">Items</th>
                        <th scope="col" className="scoreboard__th">Damage</th>
                    </tr>
                </thead>
                <tbody>
                    {players.map((player) => (
                        <PlayerRow
                            key={player.playerName}
                            player={player}
                            maxDamage={maxDamage}
                            getItemIconUrl={getItemIconUrl}
                        />
                    ))}
                </tbody>
            </table>
        </section>
    );
}

function PlayerRow({
    player,
    maxDamage,
    getItemIconUrl,
}: {
    player: PlayerStatsDto;
    maxDamage: number;
    getItemIconUrl: (name: string) => string | null;
}) {
    const damagePercent = Math.round((player.damageDealtToChampions / maxDamage) * 100);
    const kdaRatio = player.deaths === 0
        ? "Perfect"
        : ((player.kills + player.assists) / player.deaths).toFixed(1);

    // itemIds is semicolon-separated item names from Leaguepedia
    const items = player.itemIds
        ? player.itemIds.split(";").filter(Boolean)
        : [];
    const trinket = player.trinketId ?? null;

    return (
        <tr className="scoreboard__row">
            <td className="scoreboard__player-cell">
                <div className="scoreboard__player-inner">
                    <ChampionIcon champion={player.champion} size={32} />
                    <div className="scoreboard__player-info">
                        <span className="scoreboard__player-name">{player.playerName.replace(/\s*\(.*?\)/, "").trim()}</span>
                        <span className="scoreboard__player-champ">{player.champion}</span>
                    </div>
                </div>
            </td>

            <td
                className="scoreboard__kda-cell"
                aria-label={`${player.kills} kills, ${player.deaths} deaths, ${player.assists} assists, ratio ${kdaRatio}`}
            >
                <div className="scoreboard__kda-inner">
                    <span className="scoreboard__kda">
                        <span className="scoreboard__kda-kills">{player.kills}</span>
                        <span className="scoreboard__kda-deaths">{player.deaths}</span>
                        <span className="scoreboard__kda-assists">{player.assists}</span>
                    </span>
                    <span className="scoreboard__kda-ratio">{kdaRatio}</span>
                </div>
            </td>

            <td className="scoreboard__cs-cell">
                <span className="scoreboard__cs">{player.creepScore}</span>
            </td>

            <td className="scoreboard__gold-cell">
                <span className="scoreboard__gold">{formatGold(player.goldEarned)}</span>
            </td>

            <td className="scoreboard__items-cell">
                <div className="scoreboard__items-inner">
                    {items.map((itemName, slotIndex) => (
                        <ItemIcon
                            key={`${player.playerName}-item-${slotIndex}`}
                            name={itemName}
                            iconUrl={getItemIconUrl(itemName)}
                        />
                    ))}
                    {trinket && (
                        <ItemIcon
                            name={trinket}
                            iconUrl={getItemIconUrl(trinket)}
                            isTrinket
                        />
                    )}
                </div>
            </td>

            <td className="scoreboard__damage-cell">
                <div className="scoreboard__damage-inner">
                    <span className="scoreboard__damage-num">
                        {formatDamage(player.damageDealtToChampions)}
                    </span>
                    <div className="scoreboard__damage-bar-track" aria-hidden="true">
                        <div
                            className="scoreboard__damage-bar-fill"
                            style={{ width: `${damagePercent}%` }}
                        />
                    </div>
                </div>
            </td>
        </tr>
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
    if (!iconUrl) {
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
                onError={(e) => {
                    (e.target as HTMLImageElement).style.opacity = "0";
                }}
            />
        </div>
    );
}

function ChampionIcon({ champion, size }: { champion: string; size: number }) {
    const normalized = champion
        .replace(/[^a-zA-Z0-9]/g, "")
        .replace(/^(.)/, (c) => c.toUpperCase());

    const overrides: Record<string, string> = {
        "Wukong": "MonkeyKing",
        "Nunu": "Nunu",
        "Renata": "Renata",
    };

    const ddName = overrides[champion] ?? normalized;
    const url = `https://ddragon.leagueoflegends.com/cdn/15.8.1/img/champion/${ddName}.png`;

    return (
        <img
            src={url}
            alt=""
            width={size}
            height={size}
            className="scoreboard__champ-icon"
            onError={(e) => {
                (e.target as HTMLImageElement).style.display = "none";
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
