import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Navigate, Outlet, Route, Routes } from "react-router-dom";
import { Navbar } from "@/components/Navbar.tsx";
import { SiteFooter } from "@/components/SiteFooter.tsx";
import { Admin } from "@/routes/Admin.tsx";
import { Home } from "@/routes/Home.tsx";
import { League } from "@/routes/League.tsx";
import { MatchDetail } from "@/routes/MatchDetail.tsx";

const queryClient = new QueryClient();

/** Global chrome: header, single document main (scroll surface), footer. */
function AppShell() {
    return (
        <div className="app-shell">
            <header className="site-header">
                <Navbar />
            </header>
            <main className="app-main">
                <Outlet />
            </main>
            <SiteFooter />
        </div>
    );
}

function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <BrowserRouter>
                <Routes>
                    <Route element={<AppShell />}>
                        <Route path="/" element={<Home />} />
                        <Route path="/admin" element={<Admin />} />
                        <Route path="/matches/:id" element={<MatchDetail />} />
                        <Route path="/leagues" element={<Navigate to="/" replace />} />
                        <Route path="/leagues/:shortName" element={<League />} />
                        <Route path="/standings" element={<div />} />
                        <Route path="/teams" element={<div />} />
                    </Route>
                </Routes>
            </BrowserRouter>
        </QueryClientProvider>
    );
}

export default App;
