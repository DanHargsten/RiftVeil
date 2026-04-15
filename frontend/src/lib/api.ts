/** Fetches JSON from a relative API endpoint (e.g., /api/...). */
export async function fetchApi<T>(endpoint: string): Promise<T> {
  const response = await fetch(endpoint);

  if (!response.ok) {
    throw new Error(`API error: ${response.status}`);
  }

  return response.json();
}

/** Single game within a match (e.g., Game 1 of a Bo3). */
export interface GameListItem {
  id: number;
  gameNumber: number;
  winningTeam: number | null;
  vodUrl: string | null;
}

/** Match summary for list views. */
export interface MatchListItem {
  id: number;
  tournamentId: number;
  tournamentName: string;
  tournamentStage?: string;
  leagueName: string;
  leagueShortName: string;
  team1Name: string;
  team2Name: string;
  team1ShortName: string;
  team2ShortName: string;
  startsAtUtc: string;
  bestOf: number;
  status: "Scheduled" | "Live" | "Finished" | "Cancelled";
  team1Score?: number;
  team2Score?: number;
  round?: string;
  games: GameListItem[];
}

/** Full match details including tournament and extended metadata. */
export interface MatchDetails extends MatchListItem {
  startedAtUtc?: string;
  finishedAtUtc?: string;
  vodUrl?: string;
  tournament: {
    id: number;
    leagueId: number;
    leagueName: string;
    leagueShortName: string;
    name: string;
    stage?: string;
    startsAtUtc: string;
    endsAtUtc?: string;
    status: string;
  };
}

/** League summary for list views. */
export interface LeagueListItem {
  id: number;
  name: string;
  shortName: string;
  region: string | null;
  logoUrl: string | null;
}

/** Full league details including tournaments. */
export interface LeagueDetails extends LeagueListItem {
  tournaments?: Array<{
    id: number;
    name: string;
    stage?: string | null;
    startsAtUtc: string;
    endsAtUtc?: string | null;
    status: string;
  }>;
}

/** Tournament summary for list views. */
export interface TournamentListItem {
  id: number;
  leagueId: number;
  leagueName: string;
  leagueShortName: string;
  name: string;
  startsAtUtc: string;
  endsAtUtc: string;
  status: "Upcoming" | "Ongoing" | "Finished";
}

/** Full tournament details including league and matches. */
export interface TournamentDetails extends TournamentListItem {
  liquipediaSlug: string | null;
  league: LeagueListItem;
  matches: MatchListItem[];
}

/* Parameters for fetching matches with a date range. */
export interface MatchQueryParams {
  tournamentId?: number;
  from?: string;
  to?: string;
}

export const matchesApi = {
  getUpcoming: (days = 7) => 
      fetchApi<MatchListItem[]>(`/api/matches/upcoming?days=${days}`),
  
  getRecent: (count = 10) =>
      fetchApi<MatchListItem[]>(`/api/matches/recent?count=${count}`),
  
  getLive: () =>
      fetchApi<MatchListItem[]>("/api/matches/live"),

  getAll: (params?: MatchQueryParams) => {
    const qs = new URLSearchParams();
    if (params?.tournamentId) qs.set("tournamentId", String(params.tournamentId));
    if (params?.from) qs.set("from", params.from);
    if (params?.to) qs.set("to", params.to);
    const query = qs.toString();
    return fetchApi<MatchListItem[]>(`/api/matches${query ? `?${query}` : ""}`);
},

  getById: (id: number) => fetchApi<MatchDetails>(`/api/matches/${id}`),
};

export const leaguesApi = {
    getAll: () => fetchApi<LeagueListItem[]>("/api/leagues"),
    getById: (id: number) => fetchApi<LeagueDetails>(`/api/leagues/${id}`),
};

export const tournamentsApi = {
  getAll: () => fetchApi<TournamentListItem[]>("/api/tournaments"),
  getById: (id: number) => fetchApi<TournamentDetails>(`/api/tournaments/${id}`),
};