import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import {BrowserRouter, Route, Routes, Navigate} from "react-router-dom";
import {MatchDetail} from "@/routes/MatchDetail.tsx";
import {Navbar} from "@/components/Navbar.tsx";
import {League} from "@/routes/League.tsx";
import {Home} from "@/routes/Home.tsx";
import {Admin} from "@/routes/Admin.tsx";

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
                        <Route path="/admin" element={<Admin />} />
                        <Route path="/matches/:id" element={<MatchDetail />} />                        
                        <Route path="/leagues" element={<Navigate to="/" replace />} />
                        <Route path="/leagues/:shortName" element={<League />} />
                        <Route path="/standings" element={<div />} />
                        <Route path="/teams" element={<div />} />
                    </Routes>
                </main>
            </BrowserRouter>
        </QueryClientProvider>
    );
}

export default App;