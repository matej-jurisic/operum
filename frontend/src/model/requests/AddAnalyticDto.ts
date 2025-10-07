export interface AddTrackerAnalyticFieldDto {
    fieldId: string;
    purpose: string;
}

export interface AddAnalyticDto {
    code: string;
    resultType: string;
    analyticFields: AddTrackerAnalyticFieldDto[];
}
