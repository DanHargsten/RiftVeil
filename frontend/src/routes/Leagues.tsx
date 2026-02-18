import { useQuery } from "@tanstack/react-query";
import { leaguesApi } from "@/lib/api";

/** Leagues page: lists all leagues with region. */
export function Leagues() {
    const { data: leagues, isLoading, error } = useQuery({
        queryKey: ["leagues"],
        queryFn: () => leaguesApi.getAll()
    });
    
    if (isLoading) return <div>Loading...</div>
    if (error) return <div>Error loading leagues</div>;
    
    return (
        <div className="page">
            <table className="page-table league-table">
                <thead>
                    <tr>
                        <th>League</th>
                        <th>Region</th>
                    </tr>
                </thead>
                <tbody>
                    {leagues?.map(league => (
                        <tr key={league.id}>
                            <td className="league-table__league">
                                <img
                                    src={`/logos/leagues/${league.shortName.toLowerCase()}.png`}
                                    alt={league.shortName}
                                    className="league-table__logo"
                                    onError={(e) => {
                                        e.currentTarget.src = `/logos/leagues/placeholder.png`;
                                    }}
                                />
                                <div className="league-table__names">
                                    <span className="league-table__short">{league.shortName}</span>
                                    <span className="league-table__full">{league.name}</span>
                                </div>
                            </td>
                            <td>{league.region ?? "-"}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}