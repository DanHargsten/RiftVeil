export const ADMIN_LEAGUES = ["ALL", "LEC", "LCS", "LCK", "LPL", "CBLOL", "LCP"] as const;
export type AdminLeague = (typeof ADMIN_LEAGUES)[number];

/** Leagues included when Admin import runs with ALL selected. */
export const ADMIN_IMPORT_LEAGUES = ADMIN_LEAGUES.filter(
    (league): league is Exclude<AdminLeague, "ALL"> => league !== "ALL",
);

export const RECENT_DAYS = 7;
