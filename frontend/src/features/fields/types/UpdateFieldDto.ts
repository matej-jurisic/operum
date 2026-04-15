export interface UpdateFieldDto {
    name: string;
    description?: string;
    type: string;
    required: boolean;
    selectOptions?: string[];
}
