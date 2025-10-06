import {
    NumericChartAnalyticResultDto,
    ScatterChartAnalyticResultDto,
    SingleValueAnalyticResultDto,
} from "./AnalyticResultDto";

export interface TrackerAnalyticsResponseDto {
    singleValueAnalytics: SingleValueAnalyticResultDto[];
    numericChartAnalytics: NumericChartAnalyticResultDto[];
    scatterChartAnalytics: ScatterChartAnalyticResultDto[];
}
