import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useId, useState } from "react";
import { TeamLogo } from "@/components/common/Logos.tsx";
import { teamsApi, type TeamListItem } from "@/lib/api.ts";
import { ADMIN_LEAGUES, type AdminLeague } from "@/components/Admin/adminShared.ts";
import { countTeamDataProblems, formatAdminApiError, type TeamDataProblems } from "@/components/Admin/adminHelpers.ts";

type TeamFilter = "all" | "missing-icon" | "missing-logo" | "missing-short";

function problemFilterParams(filter: TeamFilter) {
    switch (filter) {
        case "missing-icon":
            return { missingIconLogo: true as const };
        case "missing-logo":
        case "missing-short":
            return {};
        default:
            return {};
    }
}

function filterTeamsClientSide(teams: TeamListItem[], filter: TeamFilter): TeamListItem[] {
    if (filter === "missing-logo") {
        return teams.filter((t) => !t.logoUrl?.trim());
    }
    if (filter === "missing-short") {
        return teams.filter((t) => {
            const short = t.shortName?.trim() ?? "";
            return short.length === 0 || short.toUpperCase() === "UNK";
        });
    }
    return teams;
}

function ProblemsSummary({
    problems,
    filter,
    onFilter,
}: {
    problems: TeamDataProblems;
    filter: TeamFilter;
    onFilter: (f: TeamFilter) => void;
}) {
    const items = [
        { id: "missing-icon" as const, label: "Missing icon URL", count: problems.missingIconUrl },
        { id: "missing-logo" as const, label: "Missing logo URL", count: problems.missingLogoUrl },
        { id: "missing-short" as const, label: "Missing short name", count: problems.missingShortName },
    ].filter((item) => item.count > 0);

    if (items.length === 0) {
        return null;
    }

    return (
        <div className="admin__problems" aria-label="Team data problems">
            <span className="admin__problems-title">Problems</span>
            <ul className="admin__problems-list">
                {items.map((item) => (
                    <li key={item.id}>
                        <button
                            type="button"
                            className={`admin__problem-chip admin__problem-chip--warn${filter === item.id ? " admin__problem-chip--active" : ""}`}
                            onClick={() => onFilter(filter === item.id ? "all" : item.id)}
                        >
                            {item.label}
                            <span className="admin__problem-chip-count">{item.count}</span>
                        </button>
                    </li>
                ))}
            </ul>
        </div>
    );
}

export function AdminTeamsTab() {
    const ids = useId();
    const queryClient = useQueryClient();
    const [league, setLeague] = useState<AdminLeague>("ALL");
    const [search, setSearch] = useState("");
    const [filter, setFilter] = useState<TeamFilter>("all");
    const [editingId, setEditingId] = useState<number | null>(null);
    const [draft, setDraft] = useState<Partial<TeamListItem>>({});
    const [syncFeedback, setSyncFeedback] = useState<{ id: number; ok: boolean; message: string } | null>(null);

    const { data: allTeams } = useQuery({
        queryKey: ["admin-teams-stats"],
        queryFn: () => teamsApi.getAll(),
        staleTime: 60_000,
    });

    const problems = countTeamDataProblems(allTeams ?? []);

    const { data: teamsRaw, isLoading, error } = useQuery({
        queryKey: ["admin-teams", league, search, filter],
        queryFn: () => teamsApi.getAll({
            search: search.trim() || undefined,
            leagueShortName: league === "ALL" ? undefined : league,
            ...problemFilterParams(filter),
        }),
    });

    const teams = teamsRaw ? filterTeamsClientSide(teamsRaw, filter) : teamsRaw;

    const syncMutation = useMutation({
        mutationFn: ({ id, overwrite }: { id: number; overwrite?: boolean }) =>
            teamsApi.syncLeaguepedia(id, overwrite ?? false),
        onSuccess: (_data, { id }) => {
            setSyncFeedback({ id, ok: true, message: "Synced from Leaguepedia." });
            queryClient.invalidateQueries({ queryKey: ["admin-teams"] });
            queryClient.invalidateQueries({ queryKey: ["admin-teams-stats"] });
        },
        onError: (err: Error, { id }) => {
            setSyncFeedback({ id, ok: false, message: err.message });
        },
    });

    const saveMutation = useMutation({
        mutationFn: ({ id, body }: { id: number; body: Parameters<typeof teamsApi.update>[1] }) =>
            teamsApi.update(id, body),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["admin-teams"] });
            queryClient.invalidateQueries({ queryKey: ["admin-teams-stats"] });
            setEditingId(null);
        },
    });

    const deleteMutation = useMutation({
        mutationFn: (team: TeamListItem) => teamsApi.delete(team.id),
        onSuccess: (_data, team) => {
            queryClient.invalidateQueries({ queryKey: ["admin-teams"] });
            queryClient.invalidateQueries({ queryKey: ["admin-teams-stats"] });
            setEditingId(null);
            setSyncFeedback({
                id: -1,
                ok: true,
                message: `Deleted "${team.name}".`,
            });
        },
        onError: (err: Error, team) => {
            const message = formatAdminApiError(err.message);
            setSyncFeedback({ id: team.id, ok: false, message });
            window.alert(message);
        },
    });

    function confirmDeleteTeam(team: TeamListItem) {
        const matchCount = team.matchCount ?? 0;
        if (matchCount > 0) {
            window.alert(
                `Cannot delete "${team.name}": used in ${matchCount} match(es).\n\n` +
                "Delete is only for orphan teams from a bad import. Remove or reassign those matches first.",
            );
            return;
        }

        const ok = window.confirm(
            `Delete team "${team.name}" (${team.shortName})?\n\n` +
            "This cannot be undone. You can recreate teams with a Leaguepedia import.",
        );
        if (ok) {
            deleteMutation.mutate(team);
        }
    }

    function startEdit(team: TeamListItem) {
        setEditingId(team.id);
        setDraft({
            name: team.name,
            shortName: team.shortName,
            region: team.region ?? "",
            logoUrl: team.logoUrl ?? "",
            iconLogoUrl: team.iconLogoUrl ?? "",
            externalId: team.externalId ?? "",
        });
        setSyncFeedback(null);
    }

    return (
        <section className="admin__section admin__section--wide" aria-labelledby="teams-heading">
            <h2 id="teams-heading" className="admin__section-title">Teams</h2>
            <div className="admin__form">
                <p className="admin__hint">
                    Sync LP fills profile logo URL, icon URL (square), region, short, and external id.
                    Match UI: local {"{short}-square.png"} → {"{short}.png"} → icon URL → placeholder.
                </p>

                {syncFeedback?.id === -1 && (
                    <p
                        className={syncFeedback.ok ? "admin__hint" : "admin__warning"}
                        role={syncFeedback.ok ? "status" : "alert"}
                    >
                        {syncFeedback.message}
                    </p>
                )}

                <div className="admin__teams-stats">
                    <button
                        type="button"
                        className={`admin__stat-chip${filter === "all" ? " admin__stat-chip--active" : ""}`}
                        onClick={() => setFilter("all")}
                    >
                        All teams
                        <span className="admin__stat-chip-count">{problems.total || "—"}</span>
                    </button>
                    <ProblemsSummary problems={problems} filter={filter} onFilter={setFilter} />
                </div>

                <div className="admin__teams-toolbar">
                    <fieldset className="admin__field admin__field--inline">
                        <legend className="admin__label">League</legend>
                        <div className="admin__league-buttons">
                            {ADMIN_LEAGUES.map((code) => (
                                <button
                                    key={code}
                                    type="button"
                                    className={`admin__league-btn${league === code ? " admin__league-btn--active" : ""}`}
                                    onClick={() => setLeague(code)}
                                    aria-pressed={league === code}
                                >
                                    {code}
                                </button>
                            ))}
                        </div>
                    </fieldset>

                    <label className="admin__teams-search" htmlFor={`${ids}-search`}>
                        <span className="admin__label">Search</span>
                        <input
                            id={`${ids}-search`}
                            type="search"
                            className="admin__input"
                            value={search}
                            onChange={(e) => setSearch(e.target.value)}
                            placeholder="Name, short name, region…"
                        />
                    </label>
                </div>

                {isLoading && <p className="admin__hint">Loading teams…</p>}
                {error && <p className="admin__warning" role="alert">{String(error)}</p>}

                {teams && teams.length > 0 && (
                    <div className="admin__teams-table-wrap">
                        <table className="admin__teams-table">
                            <thead>
                                <tr>
                                    <th scope="col">
                                        <span className="sr-only">Logo</span>
                                    </th>
                                    <th scope="col">Name</th>
                                    <th scope="col">Short</th>
                                    <th scope="col">Region</th>
                                    <th scope="col">Logo URL</th>
                                    <th scope="col">Icon URL</th>
                                    <th scope="col">External id</th>
                                    <th scope="col">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {teams.map((team) => {
                                    const isEditing = editingId === team.id;
                                    const missingIcon = !team.iconLogoUrl?.trim();
                                    const missingLogo = !team.logoUrl?.trim();
                                    const short = team.shortName?.trim() ?? "";
                                    const missingShort = short.length === 0 || short.toUpperCase() === "UNK";
                                    const rowWarn = missingIcon || missingLogo || missingShort;
                                    return (
                                        <tr key={team.id} className={rowWarn ? "admin__teams-row--warn" : undefined}>
                                            <td>
                                                <TeamLogo
                                                    shortName={team.shortName}
                                                    logoUrl={team.logoUrl}
                                                    iconLogoUrl={team.iconLogoUrl}
                                                    size={28}
                                                />
                                            </td>
                                            <td>{isEditing ? (
                                                <input
                                                    className="admin__input admin__input--cell"
                                                    value={draft.name ?? ""}
                                                    onChange={(e) => setDraft((d) => ({ ...d, name: e.target.value }))}
                                                />
                                            ) : team.name}
                                            </td>
                                            <td>{isEditing ? (
                                                <input
                                                    className="admin__input admin__input--cell"
                                                    value={draft.shortName ?? ""}
                                                    onChange={(e) => setDraft((d) => ({ ...d, shortName: e.target.value }))}
                                                />
                                            ) : (
                                                <span className={missingShort ? "admin__cell-warn" : undefined}>
                                                    {team.shortName || "—"}
                                                </span>
                                            )}
                                            </td>
                                            <td>{isEditing ? (
                                                <input
                                                    className="admin__input admin__input--cell"
                                                    value={draft.region ?? ""}
                                                    onChange={(e) => setDraft((d) => ({ ...d, region: e.target.value }))}
                                                />
                                            ) : (team.region ?? "—")}
                                            </td>
                                            <td className="admin__teams-url">
                                                {isEditing ? (
                                                    <input
                                                        className="admin__input admin__input--cell"
                                                        value={draft.logoUrl ?? ""}
                                                        onChange={(e) => setDraft((d) => ({ ...d, logoUrl: e.target.value }))}
                                                    />
                                                ) : (
                                                    <span className={missingLogo ? "admin__cell-warn" : undefined} title={team.logoUrl ?? undefined}>
                                                        {team.logoUrl ? "Set" : "—"}
                                                    </span>
                                                )}
                                            </td>
                                            <td className="admin__teams-url">
                                                {isEditing ? (
                                                    <input
                                                        className="admin__input admin__input--cell"
                                                        value={draft.iconLogoUrl ?? ""}
                                                        onChange={(e) => setDraft((d) => ({ ...d, iconLogoUrl: e.target.value }))}
                                                    />
                                                ) : (
                                                    <span className={missingIcon ? "admin__cell-warn" : undefined} title={team.iconLogoUrl ?? undefined}>
                                                        {team.iconLogoUrl ? "Set" : "—"}
                                                    </span>
                                                )}
                                            </td>
                                            <td>{isEditing ? (
                                                <input
                                                    className="admin__input admin__input--cell"
                                                    value={draft.externalId ?? ""}
                                                    onChange={(e) => setDraft((d) => ({ ...d, externalId: e.target.value }))}
                                                />
                                            ) : (team.externalId ?? "—")}
                                            </td>
                                            <td>
                                                <div className="admin__teams-row-actions">
                                                    {isEditing ? (
                                                        <>
                                                            <button
                                                                type="button"
                                                                className="admin__mini-btn"
                                                                disabled={saveMutation.isPending}
                                                                onClick={() => saveMutation.mutate({
                                                                    id: team.id,
                                                                    body: {
                                                                        name: draft.name,
                                                                        shortName: draft.shortName,
                                                                        region: draft.region,
                                                                        logoUrl: draft.logoUrl,
                                                                        iconLogoUrl: draft.iconLogoUrl,
                                                                        externalId: draft.externalId,
                                                                    },
                                                                })}
                                                            >
                                                                Save
                                                            </button>
                                                            <button
                                                                type="button"
                                                                className="admin__mini-btn admin__mini-btn--muted"
                                                                onClick={() => setEditingId(null)}
                                                            >
                                                                Cancel
                                                            </button>
                                                        </>
                                                    ) : (
                                                        <>
                                                            <button
                                                                type="button"
                                                                className="admin__mini-btn"
                                                                disabled={syncMutation.isPending}
                                                                onClick={() => syncMutation.mutate({ id: team.id })}
                                                            >
                                                                Sync LP
                                                            </button>
                                                            <button
                                                                type="button"
                                                                className="admin__mini-btn admin__mini-btn--muted"
                                                                onClick={() => startEdit(team)}
                                                            >
                                                                Edit
                                                            </button>
                                                            <button
                                                                type="button"
                                                                className="admin__mini-btn admin__mini-btn--danger"
                                                                disabled={deleteMutation.isPending || (team.matchCount ?? 0) > 0}
                                                                title={(team.matchCount ?? 0) > 0
                                                                    ? `Used in ${team.matchCount} match(es) — cannot delete`
                                                                    : "Delete orphan team row"}
                                                                onClick={() => confirmDeleteTeam(team)}
                                                            >
                                                                Delete
                                                            </button>
                                                        </>
                                                    )}
                                                </div>
                                                {syncFeedback?.id === team.id && (
                                                    <p
                                                        className={syncFeedback.ok ? "admin__hint" : "admin__warning"}
                                                        role={syncFeedback.ok ? "status" : "alert"}
                                                    >
                                                        {syncFeedback.message}
                                                    </p>
                                                )}
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                )}

                {teams && teams.length === 0 && !isLoading && (
                    <p className="admin__hint">No teams match this filter.</p>
                )}
            </div>
        </section>
    );
}
