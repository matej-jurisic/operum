import { FieldDto } from "./FieldDto";

export interface ViewDto {
    id: string;
    name: string;
    description?: string;
    sorts: ViewSortDto[];
}

export interface ViewSortDto {
    fieldId: string;
    descending: boolean;
    order: number;
    field: FieldDto;
}
