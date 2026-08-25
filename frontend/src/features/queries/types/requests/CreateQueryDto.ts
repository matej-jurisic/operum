import { QueryKind } from "../../../../shared/constants/QueryKinds";

export interface CreateQueryDto {
    kind: QueryKind;
    fieldId: string;
    /** Filters only. */
    operator?: string;
    /** Filters only. A missing value means "has no value". */
    value?: string;
    /** Sorts only. */
    descending?: boolean;
}
