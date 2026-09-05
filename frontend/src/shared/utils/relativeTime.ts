import dayjs from "dayjs";

/**
 * Short, compact "time ago" label: "just now", "5m ago", "3h ago", "2d ago", then
 * an absolute date past 30 days. Used wherever a timestamp is shown next to a row
 * rather than in a detail view.
 */
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
