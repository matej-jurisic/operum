import { OperatorTypes } from "../constants/DataTypes";
import { resolveDynamicDateToken } from "../constants/dynamicDateTokens";

function timeSpanToSeconds(value: string): number | null {
    const parts = value.split(":");
    if (parts.length < 2) return null;
    const [hours, minutes, seconds] = parts.map(Number);
    if ([hours, minutes, seconds].some((n) => Number.isNaN(n))) return null;
    return hours * 3600 + minutes * 60 + (seconds || 0);
}

function compareOrdered(fieldValue: number, operator: string, filterValue: number): boolean {
    switch (operator) {
        case OperatorTypes.Equals: return fieldValue === filterValue;
        case OperatorTypes.NotEquals: return fieldValue !== filterValue;
        case OperatorTypes.GreaterThan: return fieldValue > filterValue;
        case OperatorTypes.GreaterThanOrEqual: return fieldValue >= filterValue;
        case OperatorTypes.LessThan: return fieldValue < filterValue;
        case OperatorTypes.LessThanOrEqual: return fieldValue <= filterValue;
        default: return false;
    }
}

function matchesString(fieldValue: string | null, operator: string, filterValue: string | null): boolean {
    if (filterValue !== null) {
        switch (operator) {
            case OperatorTypes.Equals: return fieldValue === filterValue;
            case OperatorTypes.NotEquals: return fieldValue !== filterValue;
            case OperatorTypes.Contains: return fieldValue != null && fieldValue.includes(filterValue);
            case OperatorTypes.StartsWith: return fieldValue != null && fieldValue.startsWith(filterValue);
            case OperatorTypes.EndsWith: return fieldValue != null && fieldValue.endsWith(filterValue);
            default: return false;
        }
    }
    switch (operator) {
        case OperatorTypes.Equals: return fieldValue === null;
        case OperatorTypes.NotEquals: return fieldValue !== null;
        default: return false;
    }
}

function matchesNumber(fieldValue: number | null, operator: string, filterValue: string | null): boolean {
    if (filterValue !== null) {
        const filterNum = Number(filterValue);
        if (filterValue.trim() === "" || Number.isNaN(filterNum)) return false;
        if (fieldValue === null) return operator === OperatorTypes.NotEquals;
        return compareOrdered(fieldValue, operator, filterNum);
    }
    switch (operator) {
        case OperatorTypes.Equals: return fieldValue === null;
        case OperatorTypes.NotEquals: return fieldValue !== null;
        default: return false;
    }
}

function matchesTimeSpan(fieldValue: string | null, operator: string, filterValue: string | null): boolean {
    if (filterValue !== null) {
        const filterSeconds = timeSpanToSeconds(filterValue);
        if (filterSeconds === null) return false;
        if (fieldValue === null) return operator === OperatorTypes.NotEquals;
        const fieldSeconds = timeSpanToSeconds(fieldValue);
        if (fieldSeconds === null) return false;
        return compareOrdered(fieldSeconds, operator, filterSeconds);
    }
    switch (operator) {
        case OperatorTypes.Equals: return fieldValue === null;
        case OperatorTypes.NotEquals: return fieldValue !== null;
        default: return false;
    }
}

function matchesBool(fieldValue: boolean | null, operator: string, filterValue: string | null): boolean {
    const filterStr = (filterValue ?? "false").toLowerCase();
    if (filterStr !== "true" && filterStr !== "false") return false;
    const filterBool = filterStr === "true";
    if (fieldValue === null) return operator === OperatorTypes.NotEquals;
    switch (operator) {
        case OperatorTypes.Equals: return fieldValue === filterBool;
        case OperatorTypes.NotEquals: return fieldValue !== filterBool;
        default: return false;
    }
}

function matchesDateTime(fieldValue: string | null, operator: string, filterValue: string | null): boolean {
    if (filterValue !== null) {
        const resolved = resolveDynamicDateToken(filterValue);
        const filterDate = resolved ?? new Date(filterValue);
        if (Number.isNaN(filterDate.getTime())) return false;
        if (fieldValue === null) return operator === OperatorTypes.NotEquals;
        const fieldDate = new Date(fieldValue);

        // Equality is same calendar day in the browser's local time: an approximation of the
        // server's timezone-aware day window, fine for this instant client-side preview.
        const sameDay =
            fieldDate.getFullYear() === filterDate.getFullYear() &&
            fieldDate.getMonth() === filterDate.getMonth() &&
            fieldDate.getDate() === filterDate.getDate();

        switch (operator) {
            case OperatorTypes.Equals: return sameDay;
            case OperatorTypes.NotEquals: return !sameDay;
            case OperatorTypes.GreaterThan: return fieldDate.getTime() > filterDate.getTime();
            case OperatorTypes.GreaterThanOrEqual: return fieldDate.getTime() >= filterDate.getTime();
            case OperatorTypes.LessThan: return fieldDate.getTime() < filterDate.getTime();
            case OperatorTypes.LessThanOrEqual: return fieldDate.getTime() <= filterDate.getTime();
            default: return false;
        }
    }
    switch (operator) {
        case OperatorTypes.Equals: return fieldValue === null;
        case OperatorTypes.NotEquals: return fieldValue !== null;
        default: return false;
    }
}

/**
 * Client-side mirror of the server's EntryFilterMatcher, kept in sync by hand. Used to preview
 * field visibility instantly on the create-entry form without a round trip; not authoritative.
 */
export function evaluateVisibilityCondition(
    targetType: string,
    currentRaw: string,
    operator: string,
    conditionValue: string | undefined,
): boolean {
    const fieldValue = currentRaw === "" ? null : currentRaw;
    const filterValue = conditionValue === undefined || conditionValue === "" ? null : conditionValue;

    switch (targetType) {
        case "string":
        case "reference":
            return matchesString(fieldValue, operator, filterValue);
        case "number":
            return matchesNumber(fieldValue === null ? null : Number(fieldValue), operator, filterValue);
        case "bool":
            return matchesBool(fieldValue === null ? null : fieldValue.toLowerCase() === "true", operator, filterValue);
        case "timespan":
            return matchesTimeSpan(fieldValue, operator, filterValue);
        case "date":
        case "datetime":
            return matchesDateTime(fieldValue, operator, filterValue);
        default:
            return false;
    }
}
