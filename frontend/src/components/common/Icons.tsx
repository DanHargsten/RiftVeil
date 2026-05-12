type IconProps = {
    size?: number;
    className?: string;
    title?: string;
};

/** Play icon for VOD links. */
export function PlayIcon({ size = 30, className = "", title }: IconProps) {
    return (
        <svg
            width={size}
            height={size}
            viewBox="0 0 20 20"
            className={className}
            aria-hidden={title ? undefined : true}
            role={title ? "img" : "presentation"}
            fill="currentColor"
            style={{ display: "block" }}
        >
            {title ? <title>{title}</title> : null}
            <path d="M6.3 2.841A1.5 1.5 0 0 0 4 4.11v11.78a1.5 1.5 0 0 0 2.3 1.269l9.344-5.89a1.5 1.5 0 0 0 0-2.538L6.3 2.84Z" />
        </svg>
    );
}

/** Chevron/dropdown icon for expandable sections. */
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
    );
}

/** Eye-off icon for hidden spoiler state. */
export function VisibilityOffIcon({ size = 32, className = "", title }: IconProps) {
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
            <path d="m637-425-62-62q4-38-23-65.5T487-576l-62-62q13-5 27-7.5t28-2.5q70 0 119 49t49 119q0 14-2.5 28t-8.5 27Zm133 133-52-52q36-28 65.5-61.5T833-480q-49-101-144.5-158.5T480-696q-26 0-51 3t-49 10l-58-58q38-15 77.5-21t80.5-6q143 0 261.5 77.5T912-480q-22 57-58.5 103.5T770-292Zm-2 202L638-220q-38 14-77.5 21t-80.5 7q-143 0-261.5-77.5T48-480q22-57 58-104t84-85L90-769l51-51 678 679-51 51ZM241-617q-35 28-65 61.5T127-480q49 101 144.5 158.5T480-264q26 0 51-3.5t50-9.5l-45-45q-14 5-28 7.5t-28 2.5q-70 0-119-49t-49-119q0-14 3.5-28t6.5-28l-81-81Zm287 89Zm-96 96Z" />
        </svg>
    );
}

/** Eye icon for revealed spoiler state. */
export function VisibilityOnIcon({ size = 24, className = "", title }: IconProps) {
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
            <path d="M599-361q49-49 49-119t-49-119q-49-49-119-49t-119 49q-49 49-49 119t49 119q49 49 119 49t119-49Zm-187-51q-28-28-28-68t28-68q28-28 68-28t68 28q28 28 28 68t-28 68q-28 28-68 28t-68-28ZM220-270.5Q103-349 48-480q55-131 172-209.5T480-768q143 0 260 78.5T912-480q-55 131-172 209.5T480-192q-143 0-260-78.5ZM480-480Zm207 158q95-58 146-158-51-100-146-158t-207-58q-112 0-207 58T127-480q51 100 146 158t207 58q112 0 207-58Z" />
        </svg>
    );
}

export function TimeCircle({ size = 24, className = "", title }: IconProps) {
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
            <path d="M580-320 444-456.19V-648h72v162l115 115-51 51ZM444-720v-72h72v72h-72Zm276 276v-72h72v72h-72ZM444-168v-72h72v72h-72ZM168-444v-72h72v72h-72ZM480-96q-79.38 0-149.19-30T208.5-208.5Q156-261 126-330.96t-30-149.5Q96-560 126-630q30-70 82.5-122t122.46-82q69.96-30 149.5-30t149.55 30.24q70 30.24 121.79 82.08 51.78 51.84 81.99 121.92Q864-559.68 864-480q0 79.38-30 149.19T752-208.5Q700-156 629.87-126T480-96Zm.46-72q130.46 0 221-91Q792-350 792-480.46t-90.54-221Q610.92-792 480.46-792 350-792 259-701.46t-91 221Q168-350 259-259t221.46 91ZM480-480Z" />
        </svg>
    );
}