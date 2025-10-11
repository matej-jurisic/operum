export interface CreateViewDto {
    name: string;
    description?: string;
    sorts: CreateViewSortDto[];
    filters: CreateViewFilterDto[];
}

export interface CreateViewSortDto {
    fieldId: string;
    descending: boolean;
}

export interface CreateViewFilterDto {
    fieldId: string;
    operator: string;
    value?: string;
}
