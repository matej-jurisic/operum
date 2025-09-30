export interface AnalyticResultDto {
    analyticId: string;
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
    points: { x: string; y: number }[];
}
