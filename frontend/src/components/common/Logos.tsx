type LogoProps = {
    shortName: string;
    size?: number;
    className?: string;
};

export function TeamLogo({ shortName, size = 32, className }: LogoProps) {
    return (
        <img
            src={`/logos/teams/${shortName.trim().toLowerCase()}.png`}
            alt=""
            width={size}
            height={size}
            className={className ?? "match-card__team-logo"}
            onError={(e) => { e.currentTarget.src = "/logos/teams/placeholder.png"; }}
            loading="lazy"
            decoding="async"
        />
    );
}

export function LeagueLogo({ shortName, size = 28, className }: LogoProps) {
    return (
        <img
            src={`/logos/leagues/${shortName.trim().toLowerCase()}.png`}
            alt=""
            width={size}
            height={size}
            className={className ?? "match-card__league-logo"}
            onError={(e) => { e.currentTarget.src = "/logos/leagues/placeholder.png"; }}
            loading="lazy"
            decoding="async"
        />
    );
}