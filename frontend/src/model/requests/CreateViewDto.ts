export interface CreateViewDto {
    name: string;
    sorts: CreateViewSortDto[];
}

export interface CreateViewSortDto {
    fieldId: string;
    descending: boolean;
}
