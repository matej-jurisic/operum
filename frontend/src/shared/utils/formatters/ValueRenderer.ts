import {
    formatDynamicDateToken,
    isDynamicDateToken,
    resolveDynamicDateToken,
} from "../../constants/dynamicDateTokens";
import {
    formatBoolean,
    formatDateOnly,
    formatDateOnlyFromDate,
    formatDateTime,
    formatDateTimeFromDate,
    formatTimeSpan,
} from "./TypeFormatter";

export const renderValue = (type: string | undefined, value: unknown) => {
    if (typeof value === "string") {
        if ((type === "date" || type === "datetime") && isDynamicDateToken(value)) {
            return formatDynamicDateToken(value);
        }
        if (type === "date") return formatDateOnly(value);
        if (type === "datetime") return formatDateTime(value);
        if (type === "timespan") return formatTimeSpan(value);
        if (type === "bool") return formatBoolean(value);
        return value;
    }
    if (typeof value === "number") return value;
    if (typeof value === "boolean") return value ? "Yes" : "No";
    return "";
};

/** Like renderValue, but a dynamic date token resolves to the calendar date it currently
    points at instead of its relative label ("14/09/2026" rather than "Start of month"). */
export const renderResolvedDateValue = (type: string | undefined, value: unknown) => {
    if (typeof value === "string" && (type === "date" || type === "datetime")) {
        if (isDynamicDateToken(value)) {
            const resolved = resolveDynamicDateToken(value);
            if (!resolved) return renderValue(type, value);
            return type === "date"
                ? formatDateOnlyFromDate(resolved)
                : formatDateTimeFromDate(resolved);
        }
    }
    return renderValue(type, value);
};
