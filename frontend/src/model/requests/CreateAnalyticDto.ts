export interface CreateAnalyticDto {
    name: string;
    description?: string;
    analyticRequiredDataTypes: CreateAnalyticRequiredDataTypeDto[];
}

export interface CreateAnalyticRequiredDataTypeDto {
    type: string;
    purpose: string;
}
