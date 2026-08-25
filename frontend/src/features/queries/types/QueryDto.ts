import { FieldDto } from "../../fields/types/FieldDto";

export interface QueryDto {
    id: string;
    name: string;
    description?: string;
    sorts: QuerySortDto[];
    filters: QueryFilterDto[];
}

export interface QuerySortDto {
    descending: boolean;
    order: number;
    field: FieldDto;
}

export interface QueryFilterDto {
    field: FieldDto;
    operator: string;
    value: string;
}
