export interface CreateViewDto {
    name: string;
    description?: string;
    sorts: CreateViewSortDto[];
}

export interface CreateViewSortDto {
    fieldId: string;
    descending: boolean;
}
