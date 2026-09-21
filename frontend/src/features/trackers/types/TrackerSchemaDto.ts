export interface TrackerSchemaFieldDto {
    name: string;
    type: string;
    required?: boolean;
    selectOptions?: string[];
    formula?: string;
    references?: string;
}

export interface TrackerSchemaViewDto {
    name: string;
    description?: string;
    columns: string[];
    filters: { field: string; operator?: string; value?: string }[];
    sorts: { field: string; descending?: boolean }[];
}

export interface TrackerSchemaDto {
    name: string;
    description?: string;
    fields: TrackerSchemaFieldDto[];
    views: TrackerSchemaViewDto[];
}
