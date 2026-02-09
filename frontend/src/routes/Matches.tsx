import { MatchList } from "@/components/MatchList";

export function Matches() {
    return (
        <div className="page page--matches">
            <header className="page-header">
                <h1 id="matches-title" className="page-header__title">Matches</h1>
                <p className="page-header__subtitle">
                    Upcoming League of Legends matches
                </p>
            </header>
            
            <section className="page-content" aria-labelledby="matches-title">
                <MatchList />                
            </section>
                
        </div>
    );
}