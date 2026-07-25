const API_ERROR_BODY_MAX = 280;

function truncateDetail(text: string, max: number): string {
  const t = text.replace(/\s+/g, " ").trim();
  if (t.length <= max) return t;
  return `${t.slice(0, max)}…`;
}

async function parseJsonResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const bodyText = await response.text();
    const detail =
      bodyText.length > 0 ? truncateDetail(bodyText, API_ERROR_BODY_MAX) : "";
    const suffix = detail ? `: ${detail}` : "";
    throw new Error(`API error: ${response.status}${suffix}`);
  }

  return response.json();
}

/** Fetches JSON from a relative API endpoint (e.g., /api/...). */
export async function fetchApi<T>(endpoint: string): Promise<T> {
  const response = await fetch(endpoint);
  return parseJsonResponse<T>(response);
}

export async function postApi<T>(endpoint: string, body?: unknown): Promise<T> {
  const response = await fetch(endpoint, {
    method: "POST",
    headers: body !== undefined ? { "Content-Type": "application/json" } : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  return parseJsonResponse<T>(response);
}

export async function patchApi<T>(endpoint: string, body: unknown): Promise<T> {
  const response = await fetch(endpoint, {
    method: "PATCH",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  return parseJsonResponse<T>(response);
}

export async function deleteApi(endpoint: string): Promise<void> {
  const response = await fetch(endpoint, { method: "DELETE" });
  if (!response.ok) {
    const bodyText = await response.text();
    const detail =
      bodyText.length > 0 ? truncateDetail(bodyText, API_ERROR_BODY_MAX) : "";
    const suffix = detail ? `: ${detail}` : "";
    throw new Error(`API error: ${response.status}${suffix}`);
  }
}

/** Single VOD row on match detail responses. */
export interface GameVodItem {
  id: number;
  provider: string;
  source: "Imported" | "Manual";
  locale: string | null;
  url: string;
  offsetSeconds?: number | null;
  draftOffsetSeconds?: number | null;
}

/** Single game within a match (e.g., Game 1 of a Bo3). */
export interface GameListItem {
  id: number;
  gameNumber: number;
  winningTeam: number | null;
  vodUrl: string | null;
  vodBaseUrl?: string | null;
  vodDraftOffsetSeconds?: number | null;
  vodGameStartOffsetSeconds?: number | null;
  vods?: GameVodItem[] | null;
}

/** Match summary for list views. */
export interface MatchListItem {
  id: number;
  tournamentId: number;
  tournamentName: string;
  tournamentStage?: string;
  leagueName: string;
  leagueShortName: string;
  leagueRegion?: string | null;
  team1Name: string;
  team2Name: string;
  team1ShortName: string;
  team2ShortName: string;
  team1LogoUrl?: string | null;
  team2LogoUrl?: string | null;
  team1IconLogoUrl?: string | null;
  team2IconLogoUrl?: string | null;
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

/** Parameters for fetching matches with a date range. */
export interface MatchQueryParams {
  tournamentId?: number;
  from?: string;
  to?: string;
}

/** Stat summary for an individual player for a single game. */
export interface PlayerStatsDto {
  playerName: string;
  ingameRole: string;
  champion: string;
  championLevel?: number | null;
  kills: number;
  deaths: number;
  assists: number;
  goldEarned: number;
  creepScore: number;
  damageDealtToChampions: number;
  visionScore: number;
  itemIds: string | null;
  trinketId: string | null;
  summonerSpell1Id: string | null;
  summonerSpell2Id: string | null;
  teamNumber: number;
}

/** Team stat summary for a single game */
export interface TeamStatsDto {
  totalKills: number;
  totalDeaths: number;
  totalAssists: number;
  totalGoldEarned: number;
  towersDestroyed: number;
  inhibitorsDestroyed: number;
  baronsSlain: number;
  riftHeraldsSlain: number;
  voidGrubsSlain: number;
  totalDragonsSlain: number;
  infernalDragonsSlain: number;
  mountainDragonsSlain: number;
  cloudDragonsSlain: number;
  oceanDragonsSlain: number;
  hextechDragonsSlain: number;
  chemtechDragonsSlain: number;
  elderDragonsSlain: number;
  gameDurationSeconds: number;
  teamNumber: number;
}

/** Represents a single pick or ban entry in the draft phase. */
export interface DraftEntryDto {
  teamNumber: number;
  phase: "Pick" | "Ban";
  sequenceNumber: number;
  champion: string;
}

/** Full statistical details for a single game, including players and draft. */
export interface GameDetailsDto {
  gameId: number;
  gameNumber: number;
  winningTeam: number | null;
  team1Side: string | null;
  team2Side: string | null;
  vodUrl: string | null;
  team1Players: PlayerStatsDto[];
  team2Players: PlayerStatsDto[];
  team1Stats: TeamStatsDto | null;
  team2Stats: TeamStatsDto | null;
  draft: DraftEntryDto[];
  gameDurationSeconds: number | null;
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

export interface TeamListItem {
  id: number;
  name: string;
  shortName: string;
  region: string | null;
  logoUrl: string | null;
  iconLogoUrl: string | null;
  externalId: string | null;
  matchCount: number;
}

export interface TeamMissingIcon {
  id: number;
  name: string;
  shortName: string;
}

export interface TeamBackfillResult {
  total: number;
  updated: number;
  skipped: number;
  notFound: number;
  missingIconLogo: TeamMissingIcon[];
}

export interface UpdateTeamRequest {
  name?: string | null;
  shortName?: string | null;
  region?: string | null;
  logoUrl?: string | null;
  iconLogoUrl?: string | null;
  externalId?: string | null;
}

export const teamsApi = {
  getAll: (params?: { search?: string; leagueShortName?: string; missingIconLogo?: boolean }) => {
    const qs = new URLSearchParams();
    if (params?.search) qs.set("search", params.search);
    if (params?.leagueShortName) qs.set("leagueShortName", params.leagueShortName);
    if (params?.missingIconLogo) qs.set("missingIconLogo", "true");
    const query = qs.toString();
    return fetchApi<TeamListItem[]>(`/api/teams${query ? `?${query}` : ""}`);
  },
  getById: (id: number) => fetchApi<TeamListItem>(`/api/teams/${id}`),
  update: (id: number, body: UpdateTeamRequest) => patchApi<TeamListItem>(`/api/teams/${id}`, body),
  syncLeaguepedia: (id: number, overwrite = false) =>
    postApi<TeamListItem>(`/api/teams/${id}/sync-leaguepedia?overwrite=${overwrite}`),
  delete: (id: number) => deleteApi(`/api/teams/${id}`),
};

export const importBackfillApi = {
  gameIds: (leagueShortName: string) =>
    postApi<{ gamesUpdated: number; tournamentsSkipped: number }>(
      `/api/import/backfill-game-ids/${leagueShortName}`,
    ),
  gameSides: (leagueShortName: string) =>
    postApi<{ gamesUpdated: number; tournamentsSkipped: number }>(
      `/api/import/backfill-game-sides/${leagueShortName}`,
    ),
  teams: (leagueShortName?: string, overwrite = false) => {
    const base = leagueShortName && leagueShortName !== "ALL"
      ? `/api/import/backfill-teams/${leagueShortName}`
      : "/api/import/backfill-teams";
    return postApi<TeamBackfillResult>(`${base}?overwrite=${overwrite}`);
  },
};

export interface GameVodUpdateResult {
  gameId: number;
  gameNumber: number;
  vodUrl: string | null;
  baseUrl: string | null;
  draftOffsetSeconds: number | null;
  gameStartOffsetSeconds: number | null;
}

export const gamesApi = {
  getDetails: (gameId: number) =>
    fetchApi<GameDetailsDto>(`/api/games/${gameId}/details`),

  updateVod: (gameId: number, body: {
    url: string | null;
    draftOffsetSeconds?: number | null;
    gameStartOffsetSeconds?: number | null;
    offsetSeconds?: number;
  }) =>
    patchApi<GameVodUpdateResult>(`/api/games/${gameId}/vod`, body),
};
