export interface CreateAnalyticFieldDto {
    fieldId: string;
    purpose: string;
}

export interface CreateAnalyticDto {
    code: string;
    type: string;
    analyticFields: CreateAnalyticFieldDto[];
}
