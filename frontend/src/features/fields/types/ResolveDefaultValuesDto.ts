export interface ResolvedDefaultValueDto {
    fieldId: string;
    fieldName: string;
    value?: string;
}

export interface ResolvedFieldVisibilityDto {
    fieldId: string;
    fieldName: string;
    visible: boolean;
}

export interface ResolveDefaultValuesResponseDto {
    defaults: ResolvedDefaultValueDto[];
    visibility: ResolvedFieldVisibilityDto[];
}
