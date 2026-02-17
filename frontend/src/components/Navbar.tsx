import { NavLink } from "react-router-dom";

export function Navbar() {
    return (
        <nav className="navbar" aria-label="Primary">
            <div className="navbar__container container">
                <NavLink to="/" className="navbar__brand">
                    Rift Veil
                </NavLink>
                
                <div className="navbar__links">
                    <NavLink to="/" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"} end>
                        Match
                    </NavLink>

                    <NavLink to="/tournaments" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Tourna
                    </NavLink>

                    <NavLink to="/leagues" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"}>
                        Leagu
                    </NavLink>
                </div>
            </div>
        </nav>
    )
}