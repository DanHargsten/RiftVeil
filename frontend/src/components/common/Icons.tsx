type IconProps = {
    size?: number;
    className?: string;
    title?: string;
};

export function PlayIcon({ size = 24, className = "", title }: IconProps) {
    return (
        <svg
            width={size}
            height={size}
            viewBox="0 -960 960 960"
            className={className}
            aria-hidden={title ? undefined : true}
            role={title ? "img" : "presentation"}
            fill="currentColor"
            style={{ display: "block" }}
        >
            {title ? <title>{title}</title> : null}
            <path d="M320-200v-560l440 280-440 280Zm80-280Zm0 134 210-134-210-134v268Z" />
        </svg>
    );
}

export function ArrowDropdownIcon({ size = 20, className = "", title }: IconProps) {
    return (
        <svg
            width={size}
            height={size}
            viewBox="0 -960 960 960"
            className={className}
            aria-hidden={title ? undefined : true}
            role={title ? "img" : "presentation"}
            fill="currentColor"
            style={{ display: "block" }}
        >
            {title ? <title>{title}</title> : null}
            <path d="M480-384 288-576h384L480-384Z" />
            
        </svg>
    )
}