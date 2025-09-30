import {
    NumericChartAnalyticResultDto,
    SingleValueAnalyticResultDto,
} from "./AnalyticResultDto";

export interface TrackerAnalyticsResponseDto {
    singleValueAnalytics: SingleValueAnalyticResultDto[];
    numericChartAnalytics: NumericChartAnalyticResultDto[];
}
