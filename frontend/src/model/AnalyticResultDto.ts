export interface AnalyticResultDto {
    analyticId: string;
    trackerAnalyticId: string;
    name: string;
    description?: string;
    code: string;
    resultType: string;
}

export interface SingleValueAnalyticResultDto extends AnalyticResultDto {
    value: string;
    fieldName: string;
    entryId?: string;
}

export interface NumericChartAnalyticResultDto extends AnalyticResultDto {
    xFieldName: string;
    yFieldName: string;
    yFieldType: string;
    points: { x: string; y: number }[];
}

export interface ScatterPlotAnalyticResultDto extends AnalyticResultDto {
    xFieldName: string;
    yFieldName: string;
    xFieldType: string;
    yFieldType: string;
    points: { x: number; y: number }[];
}

export interface CalendarAnalyticResultDto extends AnalyticResultDto {
    dateFieldName: string;
    eventFieldName: string;
    points: {date: Date; name: string}[];
}
