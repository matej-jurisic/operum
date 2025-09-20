import {
    formatDateOnly,
    formatDateTime,
    formatTimeSpan,
} from "./TypeFormatter";

export const renderValue = (type: string | undefined, value: unknown) => {
    if (typeof value === "string") {
        if (type === "date") return formatDateOnly(value);
        if (type === "datetime") return formatDateTime(value);
        if (type === "timespan") return formatTimeSpan(value);
        return value;
    }
    if (typeof value === "number") return value;
    if (typeof value === "boolean") return value ? "Yes" : "No";
    return "";
};
