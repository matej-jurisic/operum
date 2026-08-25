import { QueryDto } from "../../queries/types/QueryDto";

export interface ViewDto {
    id: string;
    name: string;
    description?: string;
    /** Ordered: precedence for sort-merge (first-field-wins) and display order. */
    queries: QueryDto[];
}
