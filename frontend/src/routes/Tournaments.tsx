import { useQuery } from "@tanstack/react-query";
import { tournamentsApi } from "@/lib/api.ts";

/** Tournaments page: lists all tournaments. */
export function Tournaments() {
    const { data: tournaments, isLoading, error } = useQuery({
        queryKey: ["tournaments"],
        queryFn: () => tournamentsApi.getAll(),
    });

    if (isLoading) {
        return (
            <div className="page" role="status" aria-live="polite">
                <div className="match-list__state">Loading...</div>
            </div>
        );
    }
    if (error) {
        return (
            <div className="page" role="alert">
                <div className="match-list__state match-list__state--error">Error loading tournaments</div>
            </div>
        );
    }

    return (
        <div className="page">
            <table className="page-table tournament-table">
                <caption className="sr-only">Tournaments and status</caption>
                <thead>
                    <tr>
                        <th scope="col">Tournament</th>
                        <th scope="col">Status</th>
                    </tr>
                </thead>
                <tbody>
                    {tournaments?.map((tournament) => (
                        <tr key={tournament.id}>
                            <td>{tournament.name}</td>
                            <td>{tournament.status}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
