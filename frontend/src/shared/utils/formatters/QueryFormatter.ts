import { FieldDto } from "../../../features/fields/types/FieldDto";
import { OperatorTypes } from "../../constants/DataTypes";
import { fieldTypes } from "../../constants/DataTypesForSelect";
import {
    formatDynamicDateToken,
    isDynamicDateToken,
} from "../../constants/dynamicDateTokens";
import { QueryKind, QueryKinds } from "../../constants/QueryKinds";
import { formatOperator } from "./OperatorFormatter";
import { renderValue } from "./ValueRenderer";

interface Clause {
    kind: QueryKind;
    field?: FieldDto;
    operator?: string | null;
    value?: string | null;
    descending?: boolean;
}

/**
 * A query has no name of its own, so everywhere one is listed or picked it is
 * shown by what it actually does: "Weight ≥ 80", "Logged on descending".
 */
export const describeClause = (clause: Clause): string => {
    const fieldName = clause.field?.name ?? "Unknown field";

    if (clause.kind === QueryKinds.Sort)
        return `${fieldName} ${clause.descending ? "descending" : "ascending"}`;

    if (!clause.operator) return fieldName;

    // A filter with no value is how "has no value" is written down, and only the two
    // equality operators can express it.
    if (clause.value === undefined || clause.value === null || clause.value === "") {
        if (clause.operator === OperatorTypes.Equals) return `${fieldName} is empty`;
        if (clause.operator === OperatorTypes.NotEquals) return `${fieldName} has a value`;
        return `${fieldName} ${formatOperator(clause.operator)}`;
    }

    const rendered = renderValue(clause.field?.type, clause.value);
    const quoted =
        clause.field?.type === "string" ? `"${rendered}"` : `${rendered}`;

    return `${fieldName} ${formatOperator(clause.operator)} ${quoted}`;
};

const dataTypeLabel = (value: string) =>
    fieldTypes.find((t) => t.value === value)?.label ?? value;

interface AbstractClause {
    kind: string;
    dataType: string;
    operator?: string | null;
    value?: unknown;
    descending?: boolean;
}

/**
 * describeClause for a field-agnostic clause -- a DashboardView clause or a template row
 * that names only a data type, not a concrete field: "Date ≥ Start of this month".
 */
export const describeAbstractClause = (c: AbstractClause): string => {
    const type = dataTypeLabel(c.dataType);

    if (c.kind === QueryKinds.Sort)
        return `${type} ${c.descending ? "descending" : "ascending"}`;

    const operator = c.operator ? formatOperator(c.operator) : "";

    if (c.value === undefined || c.value === null || c.value === "")
        return `${type} ${operator} empty`.replace(/\s+/g, " ").trim();

    const value =
        typeof c.value === "string" && isDynamicDateToken(c.value)
            ? formatDynamicDateToken(c.value)
            : c.value instanceof Date
              ? c.value.toLocaleDateString()
              : String(c.value);

    return `${type} ${operator} ${value}`.replace(/\s+/g, " ").trim();
};
