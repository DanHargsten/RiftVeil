export async function fetchApi<T>(endpoint: string): Promise<T> {
  const response = await fetch(endpoint); // ← Just /api/...

  if (!response.ok) {
    throw new Error(`API error: ${response.status}`);
  }

  return response.json();
}

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
}

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

export const matchesApi = {
  getUpcoming: (days = 7) =>
    fetchApi<MatchListItem[]>(`/api/matches/upcoming?days=${days}`),

  getAll: () => fetchApi<MatchListItem[]>("/api/matches"),

  getById: (id: number) => fetchApi<MatchDetails>(`/api/matches/${id}`),
};
