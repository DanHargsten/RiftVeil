import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import {MatchList} from "@/components/MatchList.tsx";

const queryClient = new QueryClient();

function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <div className="app">
                <header className="app-header">
                    <div className="app-header__container">
                        <h1 className="app-header__title">Rift Veil</h1>
                        <p className="app-header__subtitle">
                            League of Legends Esports Schedule
                        </p>
                    </div>
                </header>
                
                <main className="app-main">
                    <MatchList />
                </main>
            </div>
        </QueryClientProvider>
    );
}

export default App;