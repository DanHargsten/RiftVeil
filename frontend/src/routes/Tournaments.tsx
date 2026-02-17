import { useQuery } from "@tanstack/react-query";
import { tournamentsApi } from "@/lib/api";

export function Tournaments() {
    const { data: tournaments, isLoading, error } = useQuery({
        queryKey: ["tournaments"],
        queryFn: () => tournamentsApi.getAll()
    });
    
    if (isLoading) return <div>Loading...</div>
    if (error) return <div>Error loading tournaments</div>;
    
    return (
        <div className="page">
            <table className="page-table tournament-table">
                <thead>
                    <tr>
                        <th>Tournament</th>
                        <th>Status</th>
                    </tr>                    
                </thead>
                <tbody>
                    {tournaments?.map(tournament => (
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