import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import {BrowserRouter, Route, Routes} from "react-router-dom";
import {Tournaments} from "@/routes/Tournaments.tsx";
import {MatchDetail} from "@/routes/MatchDetail.tsx";
import {Navbar} from "@/components/Navbar.tsx";
import {Leagues} from "@/routes/Leagues.tsx";
import {Home} from "@/routes/Home.tsx";

const queryClient = new QueryClient();

function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <BrowserRouter>
                <header className="site-header">
                    <Navbar />                    
                </header>
                <main className="app-main">                                                 
                    <Routes>
                        <Route path="/" element={<Home />} />
                        <Route path="/matches/:id" element={<MatchDetail />} />
                        <Route path="/tournaments" element={<Tournaments />} />
                        <Route path="/leagues" element={<Leagues />} />
                        <Route path="/standings" element={<div />} />
                        <Route path="/teams" element={<div />} />
                    </Routes>
                </main>
            </BrowserRouter>
        </QueryClientProvider>
    );
}

export default App;