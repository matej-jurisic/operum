export interface CreateAnalyticDto {
    name: string;
    description?: string;
    code: string;
    resultType: string;
    analyticTypeId: string;
    analyticRequiredDataTypes: CreateAnalyticRequiredDataTypeDto[];
}

export interface CreateAnalyticRequiredDataTypeDto {
    type: string;
    purpose: string;
}
