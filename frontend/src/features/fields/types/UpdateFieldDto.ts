export interface UpdateFieldDto {
    name: string;
    description?: string;
    type: string;
    required: boolean;
    selectOptions?: string[];
    isCalculated: boolean;
    formula?: string;
    referencedTrackerId?: string;
    referencedDisplayFieldId?: string;
    defaultValue?: string;
    defaultValueConstantId?: string;
    visibilityFieldId?: string;
    visibilityOperator?: string;
    visibilityValue?: string;
}
