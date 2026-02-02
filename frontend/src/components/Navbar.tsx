import { NavLink } from "react-router-dom";

export function Navbar() {
    return (
        <div className="navbar">
            <div className="navbar__container">
                <NavLink to="/" className="navbarName">
                    Rift Veil
                </NavLink>
                
                <div className="navbar__links">
                    <NavLink to="/" className={({ isActive }) => isActive ? "navbar__link navbar__link--active" : "navbar__link"} end>
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
        </div>
    )
}