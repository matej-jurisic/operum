import dayjs from "dayjs";

/** Compact "time ago" label; falls back to an absolute date past 30 days. */
export function relativeTime(value?: string | null): string {
    if (!value) return "";
    const then = dayjs(value);
    const minutes = dayjs().diff(then, "minute");

    if (minutes < 1) return "just now";
    if (minutes < 60) return `${minutes}m ago`;

    const hours = dayjs().diff(then, "hour");
    if (hours < 24) return `${hours}h ago`;

    const days = dayjs().diff(then, "day");
    return days < 30 ? `${days}d ago` : then.format("D MMM YYYY");
}
