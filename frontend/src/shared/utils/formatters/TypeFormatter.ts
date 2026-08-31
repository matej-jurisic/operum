export const formatDateTime = (value?: string) =>
    value
        ? new Date(value).toLocaleString("en-GB", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit",
            hour12: false,
        })
        : "";

export const formatDateTimeFromDate = (date?: Date) =>
    date
        ? date.toLocaleString("en-GB", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit",
            second: "2-digit",
            hour12: false,
        })
        : "";

export const formatDateOnly = (value?: string) =>
    value
        ? new Date(value).toLocaleDateString("en-GB", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
        })
        : "";

const RELATIVE = new Intl.RelativeTimeFormat("en", { numeric: "auto" });
const RELATIVE_STEPS: [Intl.RelativeTimeFormatUnit, number][] = [
    ["year", 60 * 60 * 24 * 365],
    ["month", 60 * 60 * 24 * 30],
    ["week", 60 * 60 * 24 * 7],
    ["day", 60 * 60 * 24],
    ["hour", 60 * 60],
    ["minute", 60],
];

/** "2 days ago", "just now" -- from an ISO date string. */
export const formatRelativeTime = (value?: string): string => {
    if (!value) return "";
    const diffSeconds = (Date.now() - new Date(value).getTime()) / 1000;
    for (const [unit, seconds] of RELATIVE_STEPS) {
        if (Math.abs(diffSeconds) >= seconds) {
            return RELATIVE.format(-Math.round(diffSeconds / seconds), unit);
        }
    }
    return "just now";
};

export const formatTimeSpan = (value?: string) => {
    if (!value) return "";
    const [hours, minutes, seconds] = value.split(":");
    return `${hours}:${minutes}:${seconds.split(".")[0]}`;
};

export const formatMinutesToTime = (minutes?: number): string => {
    if (minutes === undefined || minutes === null) return "";

    const hours = Math.floor(minutes / 60);
    const mins = Math.floor(minutes % 60);
    const seconds = Math.floor((minutes % 1) * 60);
    return `${hours.toString().padStart(2, "0")}:${mins
        .toString()
        .padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`;
};

export const formatBoolean = (value?: string): string => {
    if (value === undefined || value === null) return "";
    // Entry values arrive capitalised from .NET while a query stores the value it was
    // written with, so the comparison cannot be case-sensitive.
    return value.toLowerCase() === "true" ? "Yes" : "No";
};
