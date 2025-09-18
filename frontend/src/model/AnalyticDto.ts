export interface AnalyticDto {
    id: string;
    name: string;
    code: string;
    description?: string;
    analyticTypeId: number;
    analyticTypeName: string;
    analyticRequiredDataTypes: AnalyticRequiredDataTypeDto[];
}

export interface AnalyticRequiredDataTypeDto {
    id: string;
    type: string;
    purpose: string;
}
