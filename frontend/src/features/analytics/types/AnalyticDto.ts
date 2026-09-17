import { FieldDto } from "../../fields/types/FieldDto";

export interface AnalyticDto {
    id: string;
    name: string;
    description?: string;
    code: string;
    resultType: string;
    order?: number;
}

/** One bucket of a trend sparkline: x is the bucket's start date, y its calculated magnitude. */
export interface TrendPointDto {
    x: string;
    y: number;
}

/** Set only when the placement follows a date-bounded filter clause and hasn't turned the trend off. */
export interface TrendDto {
    points: TrendPointDto[];
    /** Same calculation over the immediately preceding, equal-length period; absent if nothing to calculate. */
    previousValue?: string;
}

export interface SingleValueAnalyticDto extends AnalyticDto {
    value: string;
    valueField?: FieldDto;
    entryId?: string;
    /** Min/Max with a Display field: the compared value, shown smaller under the label. */
    secondaryValue?: string;
    secondaryValueField?: FieldDto;
    trend?: TrendDto;
}

export const GoalDirections = {
    HigherIsBetter: "HigherIsBetter",
    LowerIsBetter: "LowerIsBetter",
} as const;

export type GoalDirection = (typeof GoalDirections)[keyof typeof GoalDirections];

export interface GoalAnalyticDto extends AnalyticDto {
    /** The calculated value, as a string in valueField's format. */
    value: string;
    /** The target, same format as value. */
    target: string;
    /** value / target, or target / value under LowerIsBetter; can exceed 1, null when target isn't a positive number. */
    progress?: number;
    valueField?: FieldDto;
    /** A cap/budget widget is LowerIsBetter. */
    direction: GoalDirection;
    trend?: TrendDto;
}

export interface LineChartAnalyticDto extends AnalyticDto {
    /** Null when the configured axis field can no longer be resolved (e.g. it was deleted). */
    xField?: FieldDto;
    /** Null when the configured axis field can no longer be resolved (e.g. it was deleted). */
    yField?: FieldDto;
    points: { x: string; y: number }[];
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
    /** Set only by the two-tracker Correlation calculation when the join has little left to plot. */
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
    /** Undefined when the configured category field was deleted; nothing can be plotted in that case. */
    nameField?: FieldDto;
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
    /** Undefined falls back to the chart's own cycling palette. */
    color?: string;
}

export interface ComposedChartAnalyticDto extends AnalyticDto {
    series: ComposedChartSeriesDto[];
    warnings: string[];
    /** Only meaningful when at least one series draws as a line. */
    yAxisFromZero: boolean;
}
