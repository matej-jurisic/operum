import { ViewClauseDto } from "../ViewClauseDto";

export interface CreateViewDto {
    name: string;
    description?: string;
    /** Ordered: precedence for sort-merge (first-field-wins) and display order. */
    queries: ViewClauseDto[];
    /** The fields the view shows, in order. Empty means every field. */
    columnFieldIds: string[];
}
