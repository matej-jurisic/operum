import { QueryKind } from "../../../shared/constants/QueryKinds";
import { FieldDto } from "../../fields/types/FieldDto";

/** One clause of a view as the client reads it back: the field-agnostic query flattened
    together with the concrete field it is bound to. */
export interface ViewQueryDto {
    kind: QueryKind;
    dataType: string;
    field: FieldDto;
    operator?: string | null;
    value?: string | null;
    descending: boolean;
}

export interface ViewDto {
    id: string;
    name: string;
    description?: string;
    /** Ordered: precedence for sort-merge (first-field-wins) and display order. */
    queries: ViewQueryDto[];
    /**
     * The fields this view shows, in the order it shows them. Empty means every field,
     * which is what every view did before columns existed.
     */
    columnFieldIds: string[];
}
