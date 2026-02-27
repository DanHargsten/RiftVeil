import { NavLink } from "react-router-dom";

/** Primary navigation: Home, Match, Tournaments, Leagues. */
export function Navbar() {
    return (
        <nav className="navbar" aria-label="Primary">
            <div className="navbar__container container">
                <NavLink to="/" className="navbar__brand">
                    Rift Veil
                </NavLink>
                
                <div className="navbar__links">
                    <NavLink to="/matches" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Matches
                    </NavLink>

                    <NavLink to="/tournaments" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Tournaments
                    </NavLink>

                    <NavLink to="/leagues" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Leagues
                    </NavLink>
                </div>
            </div>
        </nav>
    )
}