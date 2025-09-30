export interface TrackerAnalyticDto {
    id: string;
    name: string;
    code: string;
    description?: string;
    trackerAnalyticFields: TrackerAnalyticFieldDto[];
}

export interface TrackerAnalyticFieldDto {
    id: string;
    type: string;
    purpose: string;
    fieldName: string;
}
