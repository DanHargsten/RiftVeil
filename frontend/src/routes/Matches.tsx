import { MatchList } from "@/components/MatchList";

export function Matches() {
    return (
        <div className="app">
            <header className="app-header">
                <div className="app-header__container">
                    <h1 className="app-header__title">Matches</h1>
                    <p className="app-header__subtitle">
                        Upcoming League of Legends matches
                    </p>
                </div>
            </header>

            <main className="app-main">
                <MatchList />
            </main>
        </div>
    );
}