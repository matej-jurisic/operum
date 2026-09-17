import { fieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { formatOperator } from "../../../shared/utils/formatters/OperatorFormatter";
import { FieldDto } from "../../fields/types/FieldDto";

/** Data types the shared value input renders with a date/datetime picker. */
export const DATE_TYPES = ["date", "datetime"];

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
