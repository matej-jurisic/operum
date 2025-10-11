import { FieldDto } from "../../fields/types/FieldDto";

export interface ViewDto {
    id: string;
    name: string;
    description?: string;
    sorts: ViewSortDto[];
    filters: ViewFilterDto[];
}

export interface ViewSortDto {
    descending: boolean;
    order: number;
    field: FieldDto;
}

export interface ViewFilterDto {
    field: FieldDto;
    operator: string;
    value: string;
}
