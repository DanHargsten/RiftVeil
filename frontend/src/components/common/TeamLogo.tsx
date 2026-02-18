type TeamLogoProps = {
    shortName: string;
    size?: number;
    className?: string;
};

/** Renders team logo from /logos/teams/{shortName}.png with placeholder fallback. */
export function TeamLogo({ shortName, size = 24, className }: TeamLogoProps) {
    const fileName = shortName.trim().toLowerCase();
    
    return (
        <img
            src={`/logos/teams/${fileName}.png`}
            alt={shortName}
            width={size}
            height={size}
            className={className}
            onError={(e) => {
            e.currentTarget.src = `/logos/teams/placeholder.png`;
        }}
            loading="lazy"
            decoding="async"
        />
    );
}