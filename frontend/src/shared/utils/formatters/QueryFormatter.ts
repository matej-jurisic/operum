import { FieldDto } from "../../../features/fields/types/FieldDto";
import { QueryDto } from "../../../features/queries/types/QueryDto";
import { OperatorTypes } from "../../constants/DataTypes";
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

export const describeQuery = (query: QueryDto): string => describeClause(query);
