import { FieldDto } from "../../fields/types/FieldDto";

export interface AnalyticDto {
    id: string;
    name: string;
    description?: string;
    code: string;
    resultType: string;
    order?: number;
}

export interface SingleValueAnalyticDto extends AnalyticDto {
    value: string;
    valueField?: FieldDto;
    entryId?: string;
}

export interface LineChartAnalyticDto extends AnalyticDto {
    /** Null when the configured axis field can no longer be resolved (e.g. it was deleted). */
    xField?: FieldDto;
    /** Null when the configured axis field can no longer be resolved (e.g. it was deleted). */
    yField?: FieldDto;
    points: { x: string; y: number }[];
    /** Whether the Y axis is anchored at zero (default) or fitted to the data's own range.
        Set from the placement when the chart is drawn on a dashboard. */
    yAxisFromZero: boolean;
}

export interface DonutChartAnaylticDto extends AnalyticDto {
    nameField: FieldDto;
    valueField: FieldDto;
    points: { name: string; value: number }[];
}

export interface ScatterChartAnalyticDto extends AnalyticDto {
    /** Null when the configured axis field can no longer be resolved (e.g. it was deleted). */
    xField?: FieldDto;
    /** Null when the configured axis field can no longer be resolved (e.g. it was deleted). */
    yField?: FieldDto;
    points: { x: number; y: number }[];
    /** Set only by the two-tracker Correlation calculation, when the join has little or
        nothing left to plot. Empty for an ordinary single-tracker scatter plot. */
    warnings?: string[];
}

export interface CalendarAnalyticDto extends AnalyticDto {
    whenField: FieldDto;
    whatField: FieldDto;
    points: {
        date: string;
        name: string;
        entryId: string;
        /** Set only when the calendar merges more than one tracker. */
        trackerName?: string;
        color?: string;
    }[];
}

export interface BarChartAnalyticDto extends AnalyticDto {
    nameField: FieldDto;
    valueField?: FieldDto;
    points: { name: string; value: number }[];
}

export interface ComposedChartSeriesDto {
    key: string;
    label: string;
    renderType: "line" | "bar";
    xField: FieldDto;
    valueField: FieldDto;
    points: { x: string; y: number }[];
    /** The color of the tracker this series was calculated from. Undefined falls back to
        the chart's own cycling palette. */
    color?: string;
}

export interface ComposedChartAnalyticDto extends AnalyticDto {
    series: ComposedChartSeriesDto[];
    warnings: string[];
    /** Whether the Y axis is anchored at zero (default) or fitted to the data's own range.
        Set from the placement; only meaningful when at least one series draws as a line. */
    yAxisFromZero: boolean;
}
