import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import {BrowserRouter, Route, Routes} from "react-router-dom";
import {Tournaments} from "@/routes/Tournaments.tsx";
import {Navbar} from "@/components/Navbar.tsx";
import {Leagues} from "@/routes/Leagues.tsx";
import {Matches} from "@/routes/Matches.tsx";
import {MatchDetail} from "@/routes/MatchDetail.tsx";
import {Home} from "@/routes/Home.tsx";

const queryClient = new QueryClient();

function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <BrowserRouter>
                <header>
                    <Navbar />                    
                </header>
                <main className="app-main">
                    <div className="container">                                                    
                        <Routes>
                            <Route path="/" element={<Home />} />
                            <Route path="/matches" element={<Matches />} />
                            <Route path="/matches/:id" element={<MatchDetail />} />
                            <Route path="/tournaments" element={<Tournaments />} />
                            <Route path="/leagues" element={<Leagues />} />
                        </Routes>                            
                    </div>                        
                </main>
            </BrowserRouter>
        </QueryClientProvider>
    );
}

export default App;