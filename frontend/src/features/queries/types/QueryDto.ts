import { QueryKind } from "../../../shared/constants/QueryKinds";
import { FieldDto } from "../../fields/types/FieldDto";

/**
 * A single clause: a filter (field/operator/value) or a sort (field/descending),
 * told apart by `kind`. The half the kind does not use is left at its default.
 */
export interface QueryDto {
    id: string;
    kind: QueryKind;
    field: FieldDto;
    operator?: string | null;
    value?: string | null;
    descending: boolean;
}
