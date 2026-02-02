import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import {BrowserRouter, Route, Routes} from "react-router-dom";
import {Tournaments} from "@/routes/Tournaments.tsx";
import {Navbar} from "@/components/Navbar.tsx";
import {Matches} from "@/routes/Matches.tsx";
import {Leagues} from "@/routes/Leagues.tsx";

const queryClient = new QueryClient();

function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <BrowserRouter>
                <Navbar />
                <Routes>
                    <Route path="/" element={<Matches />} />
                    <Route path="/tournaments" element={<Tournaments />} />
                    <Route path="/leagues" element={<Leagues />} />
                </Routes>
            </BrowserRouter>
        </QueryClientProvider>
    );
}

export default App;