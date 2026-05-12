import { useQuery } from "@tanstack/react-query";
import { leaguesApi } from "@/lib/api.ts";

/** Leagues page: lists all leagues with region. */
export function Leagues() {
    const { data: leagues, isLoading, error } = useQuery({
        queryKey: ["leagues"],
        queryFn: () => leaguesApi.getAll(),
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
                <div className="match-list__state match-list__state--error">Error loading leagues</div>
            </div>
        );
    }

    return (
        <div className="page">
            <table className="page-table league-table">
                <caption className="sr-only">Leagues and regions</caption>
                <thead>
                    <tr>
                        <th scope="col">League</th>
                        <th scope="col">Region</th>
                    </tr>
                </thead>
                <tbody>
                    {leagues?.map((league) => (
                        <tr key={league.id}>
                            <td className="league-table__league">
                                <img
                                    src={`/logos/leagues/${league.shortName.toLowerCase()}.png`}
                                    alt=""
                                    className="league-table__logo"
                                    aria-hidden="true"
                                    onError={(e) => {
                                        e.currentTarget.src = "/logos/leagues/placeholder.png";
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
