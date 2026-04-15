export interface CreateFieldDto {
    name: string;
    description?: string;
    type: string;
    required: boolean;
    selectOptions?: string[];
}
