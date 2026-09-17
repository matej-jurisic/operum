import { CreateAnalyticFieldDto } from "../../analytics/types/requests/CreateAnalyticDto";

export interface WidgetSourceFieldDto {
    purpose: string;
    fieldId: string;
    fieldName: string;
}

export interface WidgetSourceDto {
    id: string;
    /** The widget's definition read through this source's fields, e.g. "Monthly Totals: Day, Amount". */
    name: string;
    fields: WidgetSourceFieldDto[];
    trackerId: string;
    trackerName: string;
    order: number;
}

/** Not scoped to any dashboard; see DashboardWidgetDto for how a placement of this renders on a board. */
export interface WidgetDto {
    id: string;
    name: string;
    description?: string;
    resultType: string;
    code: string;
    /** Line/Bar only: how the axis field is bucketed before the code aggregates it. */
    grouping?: string;
    /** Combined charts only: restricts to x-axis values shared by every source. */
    matchedValuesOnly: boolean;
    /** Goal widgets only: the target, in the value field's format (a number, or hh:mm:ss for a duration). */
    goalTarget?: string;
    /** Goal widgets only: a GoalDirection value. Absent behaves as HigherIsBetter. */
    goalDirection?: string;
    sources: WidgetSourceDto[];
}

export interface EntriesWidgetDefinitionDto {
    id: string;
    name: string;
    trackerId: string;
    trackerName: string;
}

export interface CreateWidgetSourceRequestDto {
    trackerId: string;
    fields: CreateAnalyticFieldDto[];
}

export interface CreateWidgetDto {
    name?: string;
    description?: string;
    resultType: string;
    code: string;
    /** Line/Bar only: how the axis field is bucketed before the code aggregates it. */
    grouping?: string;
    matchedValuesOnly?: boolean;
    /** Goal widgets only, and required for them: a number or an hh:mm:ss duration. */
    goalTarget?: string;
    /** Goal widgets only: a GoalDirection value. Omitted behaves as HigherIsBetter. */
    goalDirection?: string;
    sources: CreateWidgetSourceRequestDto[];
}

/** Result type, code, sources, and field mapping are fixed at creation; create a new widget to change them. */
export interface UpdateWidgetDto {
    name?: string;
    description?: string;
    /** Goal widgets only. Omitted, the current target is kept. */
    goalTarget?: string;
    /** Goal widgets only. Omitted, the current direction is kept. */
    goalDirection?: string;
}

export interface CreateEntriesWidgetDto {
    trackerId: string;
    name?: string;
}

export interface UpdateEntriesWidgetDto {
    name?: string;
}
