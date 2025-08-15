export interface AnalyticDto {
    id: string;
    name: string;
    description?: string;
    analyticRequiredDataTypes: AnalyticRequiredDataTypeDto[];
}

export interface AnalyticRequiredDataTypeDto {
    id: string;
    type: string;
    purpose: string;
}
