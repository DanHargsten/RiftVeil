export async function fetchApi<T>(endpoint: string): Promise<T> {
    const response = await fetch(endpoint);  // ← Just /api/...

    if (!response.ok) {
        throw new Error(`API error: ${response.status}`);
    }

    return response.json();
}

export interface MatchListItem {
    id: number;
    tournamentId: number;
    tournamentName: string;
    team1Name: string;
    team2Name: string;
    startsAtUtc: string;
    bestOf: number;
    status: 'Scheduled' | 'Live' | 'Finished' | 'Cancelled';
}

export interface MatchDetails extends MatchListItem {
    startedAtUtc?: string;
    finishedAtUtc?: string;
    team1Score?: number;
    team2Score?: number;
    vodUrl?: string;
    tournament: {
        id: number;
        leagueId: number;
        name: string;
        startsAtUtc: string;
        endsAtUtc?: string;
        status: string;
    };
}

export const matchesApi = {
    getUpcoming: (days = 7) =>
        fetchApi<MatchListItem[]>(`/api/matches/upcoming?days=${days}`),

    getAll: () =>
        fetchApi<MatchListItem[]>('/api/matches'),

    getById: (id: number) =>
        fetchApi<MatchDetails>(`/api/matches/${id}`),
};