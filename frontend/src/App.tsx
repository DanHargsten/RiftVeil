import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import {BrowserRouter, Route, Routes} from "react-router-dom";
import {Tournaments} from "@/routes/Tournaments.tsx";
import {Navbar} from "@/components/Navbar.tsx";
import {Leagues} from "@/routes/Leagues.tsx";
import {Matches} from "@/routes/Matches.tsx";

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
                            <Route path="/" element={<Matches />} />
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