import { QueryDto } from "../../queries/types/QueryDto";

export interface ViewDto {
    id: string;
    name: string;
    description?: string;
    /** Ordered: precedence for sort-merge (first-field-wins) and display order. */
    queries: QueryDto[];
    /**
     * The fields this view shows, in the order it shows them. Empty means every field,
     * which is what every view did before columns existed.
     */
    columnFieldIds: string[];
}
