import { ViewQueryRefDto } from "./ViewQueryRefDto";

export interface CreateViewDto {
    name: string;
    description?: string;
    /** Ordered: precedence for sort-merge (first-field-wins) and display order. */
    queries: ViewQueryRefDto[];
    /** The fields the view shows, in order. Empty means every field. */
    columnFieldIds: string[];
}
