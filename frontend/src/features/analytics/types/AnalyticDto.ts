export interface AnalyticDto {
    id: string;
    name: string;
    description?: string;
    code: string;
    resultType: string;
}

export interface SingleValueAnalyticResultDto extends AnalyticDto {
    value: string;
    fieldName: string;
    entryId?: string;
}

export interface NumericChartAnalyticResultDto extends AnalyticDto {
    xFieldName: string;
    yFieldName: string;
    yFieldType: string;
    points: { x: string; y: number }[];
}

export interface ScatterPlotAnalyticResultDto extends AnalyticDto {
    xFieldName: string;
    yFieldName: string;
    xFieldType: string;
    yFieldType: string;
    points: { x: number; y: number }[];
}

export interface CalendarAnalyticResultDto extends AnalyticDto {
    dateFieldName: string;
    eventFieldName: string;
    points: { date: string; name: string; entryId: string }[];
}
