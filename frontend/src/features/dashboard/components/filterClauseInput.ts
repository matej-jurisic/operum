import { fieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { OperatorTypes } from "../../../shared/constants/DataTypes";
import {
    isDynamicDateToken,
    lookbackToAnchorToken,
} from "../../../shared/constants/dynamicDateTokens";
import { formatOperator } from "../../../shared/utils/formatters/OperatorFormatter";
import { FieldDto } from "../../fields/types/FieldDto";
import { FilterClauseDto } from "../types/DashboardDto";

/** Data types the shared value input renders with a date/datetime picker. */
export const DATE_TYPES = ["date", "datetime"];

const MS_PER_DAY = 24 * 60 * 60 * 1000;

/** Lets the shared FieldDto-keyed value input render for a data-type-only clause. Mirrors
    AbstractClauseListEditor. */
export const syntheticField = (key: string, type: string): FieldDto => ({
    id: key,
    name: "Value",
    type,
    required: false,
    isCalculated: false,
});

/** "Amount ≥", "Logged after" — the clause without a value, used as an input label. Pass
    `nameOverride` to lead with a field name instead of the data type, so two same-shape
    clauses on different fields read apart ("Due date ≤" vs "Closed date ≤"). */
export const clauseLabel = (
    dataType: string,
    operator?: string | null,
    nameOverride?: string,
) => {
    const lead =
        nameOverride ??
        fieldTypes.find((t) => t.value === dataType)?.label ??
        dataType;
    return `${lead} ${operator ? formatOperator(operator) : ""}`.trim();
};

/** The string form the backend stores a clause value in. */
export function normalizeClauseValue(value: unknown): string | null {
    if (value === undefined || value === null || value === "") return null;
    if (value instanceof Date) return value.toISOString();
    return String(value);
}

export type ClauseRow =
    | { type: "single"; clause: FilterClauseDto }
    | { type: "range"; start: FilterClauseDto; end: FilterClauseDto };

/** Pairs an adjacent ≥/≤ clause on the same date field into one "range" row, so the card can
    offer prev/next navigation across both bounds at once instead of per-input arrows. */
export function groupClauseRows(clauses: FilterClauseDto[]): ClauseRow[] {
    const rows: ClauseRow[] = [];
    for (let i = 0; i < clauses.length; i++) {
        const clause = clauses[i];
        const next = clauses[i + 1];
        const isDateRangePair =
            DATE_TYPES.includes(clause.dataType) &&
            next?.dataType === clause.dataType &&
            clause.operator === OperatorTypes.GreaterThanOrEqual &&
            next.operator === OperatorTypes.LessThanOrEqual;

        if (isDateRangePair) {
            rows.push({ type: "range", start: clause, end: next });
            i++;
        } else {
            rows.push({ type: "single", clause });
        }
    }
    return rows;
}

/** A value's literal instant, or null if it's empty or a relative token ("today:-1") --
    those have no fixed day to shift an arrow from. */
function literalDateValue(value: unknown): Date | null {
    if (value instanceof Date) return isNaN(value.getTime()) ? null : value;
    if (typeof value !== "string" || value === "") return null;
    if (isDynamicDateToken(lookbackToAnchorToken(value) ?? value)) return null;
    const parsed = new Date(value);
    return isNaN(parsed.getTime()) ? null : parsed;
}

function addDays(date: Date, days: number): Date {
    const shifted = new Date(date);
    shifted.setDate(shifted.getDate() + days);
    return shifted;
}

/** Moves a literal date value by whole calendar days, preserving its time of day. Null when
    the value isn't a literal date (empty, or a relative token). */
export function shiftByDays(value: unknown, days: number): Date | null {
    const date = literalDateValue(value);
    return date ? addDays(date, days) : null;
}

/** Slides a ≥/≤ date range forward or back by its own width, with no gap or overlap --
    "next range" after Sep 1-7 is Sep 8-14. Null when either bound isn't a literal date. */
export function shiftDateRange(
    startValue: unknown,
    endValue: unknown,
    direction: 1 | -1,
): { start: Date; end: Date } | null {
    const start = literalDateValue(startValue);
    const end = literalDateValue(endValue);
    if (!start || !end) return null;

    const spanDays = Math.round((end.getTime() - start.getTime()) / MS_PER_DAY);
    const step = direction * (spanDays + 1);
    return { start: addDays(start, step), end: addDays(end, step) };
}
