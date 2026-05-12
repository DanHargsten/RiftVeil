import { useQuery } from "@tanstack/react-query";
import { useState } from "react";
import { ArrowDropdownIcon } from "@/components/common/Icons.tsx";
import { tournamentsApi, type TournamentListItem } from "@/lib/api.ts";

type LeagueGroup = {
    leagueId: number;
    leagueName: string;
    leagueShortName: string;
    tournaments: TournamentListItem[];
};

interface TournamentSidebarProps {
    selectedTournamentId: number | null;
    onSelect: (tournamentId: number | null) => void;
}

// Sidebar with tournaments grouped by league.
export function TournamentSidebar({ selectedTournamentId, onSelect }: TournamentSidebarProps) {
    const [expandedLeagues, setExpandedLeagues] = useState<Set<number>>(new Set());

    const { data: tournaments, isLoading } = useQuery({
        queryKey: ["tournaments"],
        queryFn: () => tournamentsApi.getAll(),
    });

    const toggleLeague = (leagueId: number) => {
        setExpandedLeagues(prev => {
            const next = new Set(prev);
            if (next.has(leagueId)) {
                next.delete(leagueId);
            } else {
                next.add(leagueId);
            }
            return next;
        });
    };

    if (isLoading) {
        return (
            <aside className="sidebar">
                <div className="sidebar__loading">Loading...</div>
            </aside>
        );
    }

    const grouped = groupByLeague(tournaments ?? []);
    
    return (
        <aside className="sidebar">
            <div className="sidebar__header">
                <button
                    type="button"
                    className={`sidebar__recent-btn ${selectedTournamentId === null ? "sidebar__recent-btn--active" : ""}`}
                    aria-pressed={selectedTournamentId === null}
                    onClick={() => onSelect(null)}
                >
                    Latest matches
                </button>
            </div>

            <nav className="sidebar__nav">
                {grouped.map(group => {
                    const isExpanded = expandedLeagues.has(group.leagueId);

                    return (
                        <div key={group.leagueId} className="sidebar__league">
                            <button
                                type="button"
                                className="sidebar__league-header"
                                aria-expanded={isExpanded}
                                aria-controls={`sidebar-league-${group.leagueId}-tournaments`}
                                onClick={() => toggleLeague(group.leagueId)}
                            >
                                <img
                                    src={`/logos/leagues/${group.leagueShortName.toLowerCase()}.png`}
                                    alt=""
                                    className="sidebar__league-logo"
                                    aria-hidden="true"
                                    onError={(e) => {
                                        e.currentTarget.src = `/logos/leagues/placeholder.png`;
                                    }}
                                />
                                <span className="sidebar__league-name">
                                    {group.leagueShortName}
                                </span>
                                <ArrowDropdownIcon
                                    size={20}
                                    className={`sidebar__chevron ${isExpanded ? "sidebar__chevron--open" : ""}`}
                                />
                            </button>

                            {isExpanded && (
                                <div
                                    id={`sidebar-league-${group.leagueId}-tournaments`}
                                    className="sidebar__tournaments"
                                >
                                    {group.tournaments.map(tournament => {
                                        const isSelected = selectedTournamentId === tournament.id;
                                        const statusClass =
                                            tournament.status === "Ongoing" ? "sidebar__tournament--ongoing" :
                                                tournament.status === "Upcoming" ? "sidebar__tournament--upcoming" :
                                                    "";

                                        return (
                                            <button
                                                type="button"
                                                key={tournament.id}
                                                className={`sidebar__tournament ${statusClass} ${isSelected ? "sidebar__tournament--selected" : ""}`}
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    onSelect(tournament.id);
                                                    setExpandedLeagues(prev => {
                                                        const next = new Set(prev);
                                                        next.add(group.leagueId);
                                                        return next;
                                                    });
                                                }}
                                            >
                                                <span className="sidebar__tournament-name">
                                                    {formatTournamentName(tournament.name, group.leagueShortName)}
                                                </span>
                                                {tournament.status === "Ongoing" && (
                                                    <span className="sidebar__tournament-live">LIVE</span>
                                                )}
                                            </button>
                                        );
                                    })}
                                </div>
                            )}
                        </div>
                    );
                })}
            </nav>
        </aside>
    );
}

// Groups tournaments by league, sorted by most recent first
function groupByLeague(tournaments: TournamentListItem[]): LeagueGroup[] {
    const map = new Map<number, LeagueGroup>();

    for (const t of tournaments) {
        if (!map.has(t.leagueId)) {
            map.set(t.leagueId, {
                leagueId: t.leagueId,
                leagueName: t.leagueName,
                leagueShortName: t.leagueShortName,
                tournaments: [],
            });
        }
        map.get(t.leagueId)!.tournaments.push(t);
    }

    // Sort tournaments within each league: newest first
    for (const group of map.values()) {
        group.tournaments.sort((a, b) =>
            new Date(b.startsAtUtc).getTime() - new Date(a.startsAtUtc).getTime()
        );
    }

    return Array.from(map.values());
}

// Removes league prefix from tournament names for shorter display
function formatTournamentName(name: string, leagueShort: string): string {
    // "LEC 2026 Versus Playoffs" → "2026 Versus Playoffs"
    if (name.startsWith(leagueShort + " ")) {
        return name.slice(leagueShort.length + 1);
    }
    return name;
}
