import { QueryKind } from "../../../shared/constants/QueryKinds";

/**
 * One clause of a view as the client sends it: a filter or a sort against one of the
 * tracker's fields. The data type is taken from the field server-side.
 */
export interface ViewClauseDto {
    kind: QueryKind;
    fieldId: string;
    /** Filters only. */
    operator?: string;
    /** Filters only. A missing value means "has no value". */
    value?: string;
    /** Sorts only. */
    descending?: boolean;
}
