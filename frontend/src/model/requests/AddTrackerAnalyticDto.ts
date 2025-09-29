export interface AddTrackerAnalyticDto {
    analyticId: string;
    trackerAnalyticFields: AddTrackerAnalyticFieldDto[];
}

export interface AddTrackerAnalyticFieldDto {
    fieldId: string;
    analyticRequiredDataTypeId: string;
}
