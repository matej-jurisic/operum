import { fieldTypes } from "../../../shared/constants/DataTypesForSelect";
import { formatOperator } from "../../../shared/utils/formatters/OperatorFormatter";
import { FieldDto } from "../../fields/types/FieldDto";

/** Data types the shared value input renders with a date/datetime picker. */
export const DATE_TYPES = ["date", "datetime"];

/** A synthetic field so the shared value input (which keys off a FieldDto) can render for a
    filter clause that names only a data type. Mirrors AbstractClauseListEditor. */
export const syntheticField = (key: string, type: string): FieldDto => ({
    id: key,
    name: "Value",
    type,
    required: false,
    isCalculated: false,
});

/** "Amount ≥", "Logged after" — the clause without a value, used as an input label. */
export const clauseLabel = (dataType: string, operator?: string | null) => {
    const type = fieldTypes.find((t) => t.value === dataType)?.label ?? dataType;
    return `${type} ${operator ? formatOperator(operator) : ""}`.trim();
};

/** The string form the backend stores a clause value in. */
export function normalizeClauseValue(value: unknown): string | null {
    if (value === undefined || value === null || value === "") return null;
    if (value instanceof Date) return value.toISOString();
    return String(value);
}
