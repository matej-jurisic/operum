export interface CreateQueryDto {
    name: string;
    description?: string;
    sorts: { fieldId: string; descending: boolean }[];
    filters: {
        fieldId: string;
        operator: string;
        value?: string | number | Date;
    }[];
}
