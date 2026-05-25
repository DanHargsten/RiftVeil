/** True when an offset was explicitly configured (including 0 seconds). */
export function isVodOffsetConfigured(value: number | null | undefined): value is number {
    return value !== null && value !== undefined;
}

/** Formats seconds as a compact timestamp (e.g. 63 → "1:03", 381 → "6:21"). */
export function formatVodTimestamp(seconds: number | null | undefined): string {
    if (seconds == null)
        return "";

    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;

    if (hours > 0)
        return `${hours}:${String(minutes).padStart(2, "0")}:${String(secs).padStart(2, "0")}`;

    if (minutes > 0)
        return `${minutes}:${String(secs).padStart(2, "0")}`;

    return String(secs);
}

/**
 * Parses a timestamp field.
 * Empty = not configured (null).
 * Plain seconds, M:SS, MM:SS, or H:MM:SS = configured offset.
 * Invalid input = undefined.
 */
export function parseVodTimestamp(value: string): number | null | undefined {
    const trimmed = value.trim();
    if (trimmed.length === 0)
        return null;

    if (/^\d+$/.test(trimmed)) {
        const seconds = Number.parseInt(trimmed, 10);
        return Number.isFinite(seconds) && seconds >= 0 ? seconds : undefined;
    }

    const parts = trimmed.split(":").map((part) => part.trim());
    if (parts.length !== 2 && parts.length !== 3)
        return undefined;

    if (parts.some((part) => part.length === 0 || !/^\d+$/.test(part)))
        return undefined;

    const numbers = parts.map((part) => Number.parseInt(part, 10));
    if (numbers.some((part) => !Number.isFinite(part) || part < 0))
        return undefined;

    if (parts.length === 2) {
        const [minutes, seconds] = numbers;
        if (seconds >= 60)
            return undefined;
        return minutes * 60 + seconds;
    }

    const [hours, minutes, seconds] = numbers;
    if (minutes >= 60 || seconds >= 60)
        return undefined;

    return hours * 3600 + minutes * 60 + seconds;
}

export function vodTimestampFieldError(label: string): string {
    return `${label} must be empty or a timestamp like 1:03, 6:21, or 1:02:03.`;
}
