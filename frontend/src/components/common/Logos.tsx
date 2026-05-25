import { useEffect, useState } from "react";
import { TbdTeamIcon } from "@/components/common/Icons.tsx";
import { isTbdTeam } from "@/lib/teamDisplayUtils.ts";
import {
    resolveTeamLogoSrc,
    teamLogoFallbackSrc,
    type TeamLogoVariant,
} from "@/lib/teamLogo.ts";

type LogoProps = {
    shortName: string;
    size?: number;
    className?: string;
};

type TeamLogoProps = LogoProps & {
    logoUrl?: string | null;
    iconLogoUrl?: string | null;
    variant?: TeamLogoVariant;
};

export function TeamLogo({
    shortName,
    logoUrl,
    iconLogoUrl,
    size = 32,
    className,
    variant = "icon",
}: TeamLogoProps) {
    if (isTbdTeam(shortName)) {
        const tbdClass = [className ?? "match-card__team-logo", "team-logo--tbd"]
            .filter(Boolean)
            .join(" ");
        return <TbdTeamIcon size={size} className={tbdClass} title="To Be Decided" />;
    }

    return (
        <TeamLogoImage
            shortName={shortName}
            logoUrl={logoUrl}
            iconLogoUrl={iconLogoUrl}
            size={size}
            className={className}
            variant={variant}
        />
    );
}

function TeamLogoImage({
    shortName,
    logoUrl,
    iconLogoUrl,
    size,
    className,
    variant,
}: TeamLogoProps & { variant: TeamLogoVariant }) {
    const [src, setSrc] = useState(() =>
        resolveTeamLogoSrc(logoUrl, iconLogoUrl, shortName, variant));

    useEffect(() => {
        setSrc(resolveTeamLogoSrc(logoUrl, iconLogoUrl, shortName, variant));
    }, [logoUrl, iconLogoUrl, shortName, variant]);

    const dimensionProps =
        variant === "full" ? { height: size } : { width: size, height: size };

    return (
        <img
            src={src}
            alt=""
            {...dimensionProps}
            className={className ?? "match-card__team-logo"}
            onError={() => {
                setSrc((current) => {
                    const next = teamLogoFallbackSrc(
                        current,
                        shortName,
                        logoUrl,
                        iconLogoUrl,
                        variant,
                    );
                    return next === current ? current : next;
                });
            }}
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
